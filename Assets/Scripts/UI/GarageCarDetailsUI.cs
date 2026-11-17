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
    public Button btnHaggle;   // кнопка «Торговаться» (BALANCE.md, раздел 12.1)
    public Button btnClose;
    public TMP_InputField askingPriceInput;
    public SaleSetupPanelUI saleSetupPanel;

    [Header("Модалка торга (BALANCE.md, раздел 12.1)")]
    public HaggleManager haggleModal; // ссылка на Modal_Haggle с компонентом HaggleManager

    private CarInstance _currentCar;
    public CarInstance CurrentCar => _currentCar;
    private CarInstance _askingPriceCar;
    private int _lastSuggestedAskingPrice = -1;

    public void Open(CarInstance car)
    {
        _currentCar = car;
        if (_askingPriceCar != car)
        {
            _askingPriceCar = car;
            _lastSuggestedAskingPrice = -1;
        }
        RefreshSuggestedAskingPrice();
        gameObject.SetActive(true);
        Refresh();

        // Retention: «Осмотр машины» двигает миссию «Точный диагноз» (retention, разделы 4/6).
        RetentionManager.Instance?.OnCarInspected();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if (_currentCar == null) return;

        textCarName.text = _currentCar.data.carName;
        textFinanceInfo.text = $"Всего вложено: {(_currentCar.buyPrice + _currentCar.totalRepairPaid):N0} ₽";

        // 1. Готовность к продаже и стоимость ремонта (BALANCE.md, раздел 4)
        float readiness = PlayerData.Instance.CalculateSaleReadiness(_currentCar);
        textReadiness.text = $"Готовность к продаже: {Mathf.RoundToInt(readiness * 100)}%";

        // 2. Быстрая продажа: 60% от покупки и уже оплаченного ремонта.
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
            int cost = GetDiscountedCost(def.baseRepairCost);
            row.GetComponent<RepairRowController>().Setup(_currentCar, def, cost);
        }

        // The balance starts one repair task per car; the legacy bulk-repair button is not used.
        if (btnRepairAll != null) btnRepairAll.interactable = false;

        // 5. Кнопки
        btnQuickSell.onClick.RemoveAllListeners();
        btnQuickSell.onClick.AddListener(() => QuickSell(quickPrice));

        // Открывает модалку торга (BALANCE.md, раздел 12.1)
        if (btnHaggle != null)
        {
            btnHaggle.interactable = haggleModal != null;
            btnHaggle.onClick.RemoveAllListeners();
            btnHaggle.onClick.AddListener(() =>
            {
                int ask = DefaultAskingPrice(_currentCar);
                if (askingPriceInput != null && int.TryParse(askingPriceInput.text, out int entered))
                    ask = Mathf.Max(10, PlayerData.RoundPrice(entered));
                var listing = GetSaleListing(_currentCar, ask);
                if (saleSetupPanel != null) saleSetupPanel.Open(_currentCar, listing);
                else if (listing.buyers.Count > 0) haggleModal?.Open(_currentCar, listing.buyers[0]);
            });
        }

        btnClose.onClick.RemoveAllListeners();
        btnClose.onClick.AddListener(Close);
    }

    private void QuickSell(int price)
    {
        int finalPrice = price;

        PlayerData.Instance.money += finalPrice;
        PlayerData.Instance.totalSalesCount++;
        RepairManager.Instance?.CancelForCar(_currentCar);
        PlayerData.Instance.garage.Remove(_currentCar);

        // Retention: продажа «Сдать сразу» (не через покупателя) — раздел 6.
        RetentionManager.Instance?.OnCarSold(_currentCar, finalPrice, false, 0);

        PlayerData.Instance.TickWorld();
        UIManager.Instance.UpdateTopBar();

        Close();
        GarageManager.Instance.RefreshGarage();
        SaveManager.Instance?.SaveAll();
    }

    private int GetDiscountedCost(int baseCost)
    {
        int cost = PlayerData.Instance.CalculateRepairCost(baseCost);
        // Retention: временная скидка календаря (день 3). Пока баффа нет — цена не меняется.
        return RetentionManager.Instance != null ? RetentionManager.Instance.ApplyRepairDiscount(cost) : cost;
    }

    private static int DefaultAskingPrice(CarInstance car)
    {
        // Автоцена торга должна совпадать с рекомендованной ценой продажи.
        return PlayerData.Instance.CalculateSellPrice(car);
    }

    private void RefreshSuggestedAskingPrice()
    {
        if (_currentCar == null || askingPriceInput == null) return;
        bool stillSuggested = _lastSuggestedAskingPrice < 0 || askingPriceInput.text == _lastSuggestedAskingPrice.ToString();
        if (!stillSuggested) return;

        _lastSuggestedAskingPrice = DefaultAskingPrice(_currentCar);
        askingPriceInput.SetTextWithoutNotify(_lastSuggestedAskingPrice.ToString());
    }

    public void RefreshRepairProgress()
    {
        if (_currentCar == null || !gameObject.activeInHierarchy || PlayerData.Instance == null) return;
        RefreshSuggestedAskingPrice();
        float visual = PlayerData.Instance.CalculateRepairProgress(_currentCar);
        float readiness = PlayerData.Instance.CalculateSaleReadiness(_currentCar);
        textReadiness.text = $"Ремонт: {Mathf.RoundToInt(visual * 100f)}% · готовность к продаже: {Mathf.RoundToInt(readiness * 100f)}%";
    }

    private static SaleListingData GetSaleListing(CarInstance car, int askingPrice)
    {
        int value = PlayerData.Instance.CalculateSellPrice(car);
        var listing = car.saleListing;
        if (listing == null || !listing.initialized || listing.buyers == null || listing.buyers.Count != 3)
        {
            var personalities = new System.Collections.Generic.List<BuyerPersonality>();
            while (personalities.Count < 3)
            {
                var personality = (BuyerPersonality)Random.Range(0, 5);
                if (!personalities.Contains(personality)) personalities.Add(personality);
            }

            listing = new SaleListingData { initialized = true, askingPrice = askingPrice, fairValue = value };
            foreach (var personality in personalities)
                listing.buyers.Add(HaggleManager.CreateSession(car, askingPrice, personality));
            car.saleListing = listing;
            return listing;
        }

        if (listing.askingPrice == askingPrice && listing.fairValue == value) return listing;

        int floor = PlayerData.RoundPrice(PlayerData.Instance.CalculateAsIsPrice(car) * 1.05f);
        foreach (var buyer in listing.buyers)
        {
            buyer.fairValue = value;
            buyer.askingPrice = askingPrice;
            buyer.maxAcceptablePrice = Mathf.Min(askingPrice, PlayerData.RoundPrice(value * buyer.ceilingMult));
            float ratio = value > 0 ? (float)askingPrice / value : float.MaxValue;
            float chance = askingPrice <= 2f * value
                ? Mathf.Clamp(0.95f - 0.9f * (ratio - 1f), 0.05f, 0.95f) : 0f;
            buyer.responseChance = chance;
            bool canRespond = buyer.maxAcceptablePrice >= floor;
            buyer.responded = !buyer.finished && canRespond && Random.value < chance;
            if (buyer.currentOffer > buyer.maxAcceptablePrice)
                buyer.currentOffer = buyer.maxAcceptablePrice;
        }
        listing.askingPrice = askingPrice;
        listing.fairValue = value;
        car.saleListing = listing;
        return listing;
    }
}
