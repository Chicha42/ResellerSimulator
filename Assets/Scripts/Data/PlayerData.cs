using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    [Header("Текущее состояние")]
    public int money = 25000; // Стартовый баланс 25 000 ₽ (баланс 4.0, раздел 1)
    public int totalSalesCount = 0;
    public List<CarInstance> garage = new List<CarInstance>();

    [Header("Уровни прокачки")]
    public int diagnostLevel = 0;   // 0-4
    public int warehouseLevel = 0;  // 0-4
    public int garageLevel = 0;     // 0-4 (BALANCE.md, раздел 5.3)

    [Header("Живой рынок")]
    public float marketTrend = 1.0f; // Колеблется от 0.80 до 1.22
    public int actionTick = 0;

    [Header("Торг (BALANCE.md, раздел 12.1)")]
    public int lastHaggleSessionTick = -999; // Такт создания предыдущей сессии переговоров

    // ───────────────────────── Прокачка (BALANCE.md, раздел 5) ─────────────────────────

    // Диагност: цена перехода на уровень 1..4 (индекс = целевой уровень). BALANCE.md, раздел 5.1.
    private static readonly int[] DiagnostPrices = { 0, 15000, 40000, 100000, 300000 };
    // Склад запчастей: цена перехода на уровень 1..4. BALANCE.md, раздел 5.2.
    private static readonly int[] WarehousePrices = { 0, 20000, 60000, 150000, 400000 };
    // Гараж: цена перехода на уровень 1..4 (старт — 0, уже 2 места). BALANCE.md, раздел 5.3.
    private static readonly int[] GaragePrices = { 0, 30000, 80000, 200000, 500000 };

    public const int MaxDiagnostLevel = 4;
    public const int MaxWarehouseLevel = 4;
    public const int MaxGarageLevel = 4;

    public int DiagnostUpgradeCost => diagnostLevel < MaxDiagnostLevel ? DiagnostPrices[diagnostLevel + 1] : -1;
    public int WarehouseUpgradeCost => warehouseLevel < MaxWarehouseLevel ? WarehousePrices[warehouseLevel + 1] : -1;
    public int GarageUpgradeCost => garageLevel < MaxGarageLevel ? GaragePrices[garageLevel + 1] : -1;

    public bool TryUpgradeDiagnost()
    {
        int cost = DiagnostUpgradeCost;
        if (cost < 0 || money < cost) return false;
        money -= cost;
        diagnostLevel++;
        UIManager.Instance?.UpdateTopBar();
        return true;
    }

    public bool TryUpgradeWarehouse()
    {
        int cost = WarehouseUpgradeCost;
        if (cost < 0 || money < cost) return false;
        money -= cost;
        warehouseLevel++;
        UIManager.Instance?.UpdateTopBar();
        return true;
    }

    public bool TryUpgradeGarage()
    {
        int cost = GarageUpgradeCost;
        if (cost < 0 || money < cost) return false;
        money -= cost;
        garageLevel++;
        UIManager.Instance?.UpdateTopBar();
        return true;
    }

    // ───────────────────────── Общие правила ─────────────────────────

    // Вызывается при любом значимом действии (покупка, ремонт, продажа, обновление объявлений)
    public void TickWorld()
    {
        actionTick++;
        // Рыночный тренд: случайное блуждание ±5% за действие, диапазон 0.80–1.22 (BALANCE.md, раздел 12.2).
        // Влияет ТОЛЬКО на цену продажи (см. CalculateSellPrice), не на покупку.
        marketTrend = Mathf.Clamp(marketTrend + Random.Range(-0.05f, 0.05f), 0.80f, 1.22f);

        // Каждые 4 действия — платная стоянка 150 ₽ за каждую машину в гараже (BALANCE.md, раздел 12.4).
        if (actionTick % 4 == 0 && garage.Count > 0)
        {
            int fee = garage.Count * 150;
            money -= fee;
            Debug.Log($"Списана стоянка за {garage.Count} авто: -{fee} Р");
            UIManager.Instance?.UpdateTopBar();
        }

        SaveManager.Instance?.SaveAll();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Баланс 4.0, раздел 1: один слот на старте. Каждая покупка расширения добавляет один слот.
    public int MaxGarageSlots => 1 + garageLevel;

    // Скидка склада запчастей: 0%, 10%, 20%, 30%, 40%
    public float WarehouseDiscount => warehouseLevel switch
    {
        1 => 0.10f,
        2 => 0.20f,
        3 => 0.30f,
        4 => 0.40f,
        _ => 0.0f
    };

    // Шанс сюрприза на сделке (BALANCE.md, раздел 12.3): 16% / 12% / 8% / 4% / 0% по уровням Диагноста 0-4.
    public float SurpriseChance => Mathf.Max(0f, 0.16f - diagnostLevel * 0.04f);

    // Уровень репутации 0-5; прогресс определяется числом завершённых продаж.
    public int ReputationLevel
    {
        get
        {
            if (totalSalesCount >= 100) return 5;
            if (totalSalesCount >= 63) return 4;
            if (totalSalesCount >= 38) return 3;
            if (totalSalesCount >= 20) return 2;
            if (totalSalesCount >= 8) return 1;
            return 0;
        }
    }

    public string ReputationTitle => ReputationLevel switch
    {
        5 => "Легенда рынка",
        4 => "Автобарон",
        3 => "Мастер сделок",
        2 => "Бывалый",
        1 => "Любитель",
        _ => "Новичок"
    };

    // Бонус репутации к продаже (BALANCE.md, раздел 9)
    public float ReputationBonus => ReputationLevel switch
    {
        5 => 0.10f,
        4 => 0.08f,
        3 => 0.06f,
        2 => 0.04f,
        1 => 0.02f,
        _ => 0.0f
    };

    // ───────────────────────── Формулы (BALANCE.md, разделы 1, 4, 7, 11) ─────────────────────────

    // Стоимость ремонта с учётом скидки Склада; базовые цены дефектов хранятся в CarInstance.
    public int CalculateRepairCost(int baseRepairCost)
    {
        return RoundPrice(baseRepairCost * (1f - WarehouseDiscount));
    }

    // Готовность к продаже (0..1) — доля ВЕСА устранённых дефектов (BALANCE.md, раздел 4:
    // веса 8% / 12% / 6% / 5% / 3%). Готовность не бинарная: частичный ремонт уже поднимает цену.
    public float CalculateSaleReadiness(CarInstance car)
    {
        float totalWeight = 0f;
        float fixedWeight = 0f;

        foreach (var defect in car.defects)
        {
            totalWeight += defect.sellBonus;
            if (defect.isFixed)
            {
                fixedWeight += defect.sellBonus;
            }
        }

        return totalWeight > 0f ? fixedWeight / totalWeight : 1f;
    }

    // UI-only repair progress includes elapsed work; sale readiness changes only when a node is complete.
    public float CalculateRepairProgress(CarInstance car)
    {
        if (car == null || car.defects == null || car.defects.Count == 0) return 1f;
        float totalWeight = 0f;
        float doneWeight = 0f;
        foreach (var defect in car.defects)
        {
            totalWeight += defect.sellBonus;
            if (defect.isFixed) doneWeight += defect.sellBonus;
            else if (defect.isRepairing && RepairManager.Instance != null)
                doneWeight += defect.sellBonus * RepairManager.Instance.Progress(defect);
        }
        return totalWeight > 0f ? Mathf.Clamp01(doneWeight / totalWeight) : 1f;
    }

    // V = S × (0.6 + 0.4 × прогресс) × (1 + бонус репутации) × тренд.
    public int CalculateSellPrice(CarInstance car)
    {
        float readiness = CalculateSaleReadiness(car);
        float conditionFactor = 0.6f + 0.4f * readiness;
        float value = car.data.baseSellPrice
                      * conditionFactor
                      * (1f + ReputationBonus)
                      * marketTrend;

        return RoundPrice(value);
    }

    // Быстрая продажа возвращает 60% фактически вложенных денег и не зависит от рынка.
    public int CalculateAsIsPrice(CarInstance car)
    {
        return RoundPrice((car.buyPrice + car.totalRepairPaid) * 0.6f);
    }

    public static int RoundPrice(float value) => Mathf.RoundToInt(value / 10f) * 10;
}
