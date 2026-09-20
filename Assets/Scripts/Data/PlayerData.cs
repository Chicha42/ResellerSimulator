using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    [Header("Текущее состояние")]
    public int money = 20000;
    public int totalSalesCount = 0;
    public List<CarInstance> garage = new List<CarInstance>();

    [Header("Уровни прокачки (0-4)")]
    public int diagnostLevel = 0;
    public int warehouseLevel = 0;
    public int garageLevel = 0;
    
    [Header("Живой рынок")]
    public float marketTrend = 1.0f; // Колеблется от 0.80 до 1.22
    public int actionTick = 0;

    // Вызывается при любом значимом действии (покупка, ремонт, продажа, реролл)
    public void TickWorld()
    {
        actionTick++;
        // Сдвиг тренда рынка на ±5%
        marketTrend = Mathf.Clamp(marketTrend + Random.Range(-0.05f, 0.05f), 0.80f, 1.22f);

        // Каждые 4 действия — платная стоянка 150 ₽ за машину
        if (actionTick % 4 == 0 && garage.Count > 0)
        {
            int fee = garage.Count * 150;
            money -= fee;
            Debug.Log($"Списана стоянка за {garage.Count} авто: -{fee} Р");
            UIManager.Instance?.UpdateTopBar();
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Вместимость гаража (Старт: 2, затем 3, 4, 5, 6)
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

    // Бонус репутации к продаже (раздел 10 документа)
    public float ReputationBonus
    {
        get
        {
            if (totalSalesCount >= 40) return 0.25f; // Ур. 6 Легенда
            if (totalSalesCount >= 25) return 0.20f; // Ур. 5 Автобарон
            if (totalSalesCount >= 15) return 0.15f; // Ур. 4 Мастер сделок
            if (totalSalesCount >= 8)  return 0.10f; // Ур. 3 Бывалый
            if (totalSalesCount >= 3)  return 0.05f; // Ур. 2 Любитель
            return 0.0f;                             // Ур. 1 Новичок
        }
    }

    // Сколько дефектов видит игрок (формула из раздела 12)
    public int GetVisibleDefectsCount(int conditionPercent)
    {
        return diagnostLevel switch
        {
            0 => conditionPercent switch
            {
                >= 90 => 0,
                >= 70 => 1,
                >= 50 => 2,
                >= 25 => 2,
                _ => 3
            },
            1 => 2,
            2 => 4,
            3 => 5,
            4 => 5,
            _ => 0
        };
    }

    // Расчет цены ремонта с учетом склада
    public int CalculateRepairCost(int baseRepairCost)
    {
        return Mathf.RoundToInt(baseRepairCost * (1f - WarehouseDiscount));
    }

    // Расчет финальной цены продажи (раздел 12)
    public int CalculateSellPrice(CarInstance car)
    {
        float sellMultiplier = 1f;

        foreach (var defect in car.defects)
        {
            if (defect.isFixed)
            {
                sellMultiplier += defect.sellBonus;
            }
        }

        sellMultiplier *= (1f + ReputationBonus);
        return Mathf.RoundToInt(car.data.baseSellPrice * sellMultiplier);
    }

    // Продажа «как есть» (аварийный выход = 60% от базовой цены продажи)
    public int CalculateAsIsPrice(CarInstance car)
    {
        return Mathf.RoundToInt(car.data.baseSellPrice * 0.6f);
    }
}