using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GarageCarDetailsUI : MonoBehaviour
{
    [Header("Инфо об авто")]
    public TextMeshProUGUI textCarName;
    public TextMeshProUGUI textFinanceInfo;   // "Куплено за X ₽ · Вложено Y ₽"
    public TextMeshProUGUI textReadiness;     // "Готовность к продаже: 60%"
    public TextMeshProUGUI textQuickSalePrice;// "⚡ Сдать сразу: 15 200 ₽"
    public TextMeshProUGUI textSurpriseHint;  // Шанс сюрприза

    [Header("Список дефектов")]
    public Transform defectsContainer;
    public GameObject defectRowPrefab;

    [Header("Кнопки")]
    public Button btnRepairAll;
    public TextMeshProUGUI textRepairAllCost;
    public Button btnQuickSell;
    public Button btnClose;

    private CarInstance _currentCar;
    private Button _btnHaggle;

    public void Open(CarInstance car)
    {
        _currentCar = car;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (_currentCar == null) return;

        EnsureHaggleButton();

        textCarName.text = _currentCar.data.carName;
        textFinanceInfo.text = $"Куплено за: {_currentCar.buyPrice:N0} ₽ · Вложено: {_currentCar.totalRepairPaid:N0} ₽";

        // 1. Готовность к продаже и стоимость ремонта (BALANCE.md, раздел 4)
        float readiness = PlayerData.Instance.CalculateSaleReadiness(_currentCar);
        textReadiness.text = $"Готовность к продаже: {Mathf.RoundToInt(readiness * 100)}%";

        int totalRepairAllCost = 0;
        foreach (var def in _currentCar.defects)
        {
            if (!def.isFixed)
                totalRepairAllCost += GetDiscountedCost(def.baseRepairCost);
        }

        // 2. Быстрая продажа без торга (справедливая цена × 0.95)
        int quickPrice = PlayerData.Instance.CalculateAsIsPrice(_currentCar);
        textQuickSalePrice.text = $"Сдать сразу: {quickPrice:N0} ₽";

        // Шанс сюрприза (BALANCE.md, раздел 12.3)
        int surprisePct = Mathf.RoundToInt(PlayerData.Instance.SurpriseChance * 100f);
        if (textSurpriseHint != null)
            textSurpriseHint.text = $"Риск сюрприза на сделке: {surprisePct}%";

        // 3. Пересборка списка дефектов
        foreach (Transform child in defectsContainer) Destroy(child.gameObject);

        foreach (var def in _currentCar.defects)
        {
            GameObject row = Instantiate(defectRowPrefab, defectsContainer);
            TextMeshProUGUI rowText = row.GetComponentInChildren<TextMeshProUGUI>();
            Button rowBtn = row.GetComponentInChildren<Button>();

            int cost = GetDiscountedCost(def.baseRepairCost);

            if (def.isFixed)
            {
                rowText.text = $"✔ {def.defectName} — Отремонтировано";
                rowBtn.gameObject.SetActive(false);
            }
            else
            {
                rowText.text = $"{def.defectName} — {cost:N0} ₽";
                rowBtn.gameObject.SetActive(true);
                rowBtn.interactable = PlayerData.Instance.money >= cost;

                rowBtn.onClick.RemoveAllListeners();
                rowBtn.onClick.AddListener(() => RepairDefect(def, cost));
            }
        }

        // 4. Кнопка "Чинить всё"
        if (totalRepairAllCost > 0)
        {
            textRepairAllCost.text = $"Починить всё ({totalRepairAllCost:N0} ₽)";
            btnRepairAll.interactable = PlayerData.Instance.money >= totalRepairAllCost;
            btnRepairAll.onClick.RemoveAllListeners();
            btnRepairAll.onClick.AddListener(() => RepairAll(totalRepairAllCost));
        }
        else
        {
            textRepairAllCost.text = "✔ Все дефекты устранены";
            btnRepairAll.interactable = false;
        }

        // 5. Кнопки
        btnQuickSell.onClick.RemoveAllListeners();
        btnQuickSell.onClick.AddListener(() => QuickSell(quickPrice));

        btnClose.onClick.RemoveAllListeners();
        btnClose.onClick.AddListener(Close);
    }

    // Кнопка торговли создаётся в рантайме рядом с быстрой продажей
    private void EnsureHaggleButton()
    {
        if (_btnHaggle != null) return;
        if (btnQuickSell == null) return;

        var src = btnQuickSell.GetComponent<RectTransform>();
        _btnHaggle = UIFactory.Button(btnQuickSell.transform.parent, "Btn_Haggle", "💬 Торговаться",
            UIFactory.Blue, Color.white, src.sizeDelta, () => HaggleUI.Instance?.Open(_currentCar));

        var rt = _btnHaggle.GetComponent<RectTransform>();
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, src.sizeDelta.y + 12f);
    }

    private void RepairDefect(DefectInfo def, int cost)
    {
        if (PlayerData.Instance.money < cost) return;

        PlayerData.Instance.money -= cost;
        _currentCar.totalRepairPaid += cost;
        def.isFixed = true;

        PlayerData.Instance.TickWorld();
        UIManager.Instance.UpdateTopBar();
        Refresh();
        GarageManager.Instance.RefreshGarage();
    }

    private void RepairAll(int totalCost)
    {
        if (PlayerData.Instance.money < totalCost) return;

        PlayerData.Instance.money -= totalCost;
        _currentCar.totalRepairPaid += totalCost;

        foreach (var def in _currentCar.defects)
            def.isFixed = true;

        PlayerData.Instance.TickWorld();
        UIManager.Instance.UpdateTopBar();
        Refresh();
        GarageManager.Instance.RefreshGarage();
    }

    private void QuickSell(int price)
    {
        // Риск сюрприза на сделке (BALANCE.md, раздел 12.3)
        int finalPrice = HaggleManager.ApplySurprise(price, out float cut);

        PlayerData.Instance.money += finalPrice;
        PlayerData.Instance.totalSalesCount++;
        PlayerData.Instance.garage.Remove(_currentCar);

        PlayerData.Instance.TickWorld();
        UIManager.Instance.UpdateTopBar();

        Close();
        GarageManager.Instance.RefreshGarage();
    }

    private int GetDiscountedCost(int baseCost)
    {
        return PlayerData.Instance.CalculateRepairCost(baseCost);
    }
}
