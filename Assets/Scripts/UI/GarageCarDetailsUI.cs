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

        textCarName.text = _currentCar.data.carName;
        textFinanceInfo.text = $"Куплено за: {_currentCar.buyPrice:N0} ₽ · Вложено: {_currentCar.totalRepairPaid:N0} ₽";

        // 1. Готовность к продаже (Раздел 4)
        float totalWeight = 0f;
        float fixedWeight = 0f;
        int totalRepairAllCost = 0;

        foreach (var def in _currentCar.defects)
        {
            totalWeight += def.sellBonus;
            if (def.isFixed)
            {
                fixedWeight += def.sellBonus;
            }
            else
            {
                int cost = GetDiscountedCost(def.baseRepairCost);
                totalRepairAllCost += cost;
            }
        }

        float readiness = totalWeight > 0 ? (fixedWeight / totalWeight) : 1f;
        textReadiness.text = $"Готовность к продаже: {Mathf.RoundToInt(readiness * 100)}%";

        // 2. Расчет быстрой цены (Раздел 4)
        int quickPrice = GetQuickSalePrice(readiness);
        textQuickSalePrice.text = $"Сдать сразу: {quickPrice:N0} ₽";

        // Шанс сюрприза
        int surprisePct = Mathf.RoundToInt(Mathf.Max(0f, 0.16f - PlayerData.Instance.diagnostLevel * 0.04f) * 100f);
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
        int finalPrice = price;

        // Риск сюрприза на сделке (Раздел 5.2): срез 8–18%
        float surpriseChance = Mathf.Max(0f, 0.16f - PlayerData.Instance.diagnostLevel * 0.04f);
        if (Random.value < surpriseChance)
        {
            float cut = Random.Range(0.08f, 0.18f);
            finalPrice = Mathf.RoundToInt(finalPrice * (1f - cut));
            Debug.Log($"Придрались к мелочи на сделке: -{cut * 100:N0}%. Итог: {finalPrice:N0} ₽");
        }

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
        return Mathf.RoundToInt(baseCost * (1f - PlayerData.Instance.WarehouseDiscount));
    }

    private int GetQuickSalePrice(float readiness)
    {
        float fairValue = _currentCar.data.baseBuyPrice 
                          * (0.62f + 0.58f * readiness) 
                          * (1f + PlayerData.Instance.ReputationBonus) 
                          * PlayerData.Instance.marketTrend;

        return Mathf.RoundToInt(fairValue * 0.95f);
    }
}
