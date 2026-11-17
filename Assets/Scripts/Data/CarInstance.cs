using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CarInstance
{
    // Баланс 4.0, раздел 3: относительные доли узлов в суммарной стоимости ремонта.
    private const int BodyRepairWeight = 5;
    private const int EngineRepairWeight = 8;
    private const int SuspensionRepairWeight = 4;
    private const int WheelsRepairWeight = 3;
    private const int InteriorRepairWeight = 2;
    private const int TotalRepairWeight = 22;
    private static readonly float[] ZoneRepairRates = { 0f, 0.25f, 0.32f, 0.42f, 0.48f, 0.52f };

    public CarData data;
    public int segment = 1;

    public int conditionPercent; // 0-100% (BALANCE.md, раздел 4 — пороги 90/70/50/25)
    public string conditionName; // "Отличное", "Хорошее", "Среднее", "Плохое", "Убитое"
    public int buyPrice;         // Итоговая цена покупки (BALANCE.md, раздел 3, колонка «Покупка»)

    public List<DefectInfo> defects = new List<DefectInfo>();
    public int totalRepairPaid = 0;
    public SaleListingData saleListing;

    public CarInstance(CarData carData, int carSegment = 1)
    {
        data = carData;
        segment = carSegment;
        GenerateCar();
    }

    private void GenerateCar()
    {
        // Состояние объявления, вероятность, цена и количество дефектов заданы разделом 2.
        conditionName = RollCondition();
        RollConditionPercent();
        ApplyCondition(conditionPercent);
    }

    // Установить состояние вручную; используется системой подарка retention.
    public void ApplyCondition(int percent)
    {
        conditionPercent = Mathf.Clamp(percent, 0, 100);
        conditionName = conditionPercent >= 90 ? "Отличное"
            : conditionPercent >= 70 ? "Хорошее"
            : conditionPercent >= 50 ? "Среднее"
            : conditionPercent >= 25 ? "Плохое" : "Убитое";

        buyPrice = CalculateBuyPrice();

        int defectCount = conditionName switch
        {
            "Убитое" => Random.Range(4, 6),
            "Плохое" => Random.Range(3, 5),
            "Среднее" => Random.Range(2, 4),
            "Хорошее" => Random.Range(1, 3),
            _ => Random.Range(0, 2)
        };
        GenerateRandomDefects(defectCount);
    }

    // Подарочная машина retention: хорошее состояние, покупка по базовой цене ассета.
    public static CarInstance CreateGiftCar(CarData carData, int carSegment)
    {
        var car = new CarInstance(carData, carSegment);
        car.ApplyCondition(Random.Range(70, 90));
        car.buyPrice = carData.baseBuyPrice;
        return car;
    }

    private string RollCondition()
    {
        float roll = Random.value * 100f;
        float[] chances = segment switch
        {
            1 => new[] { 0f, 0f, 20f, 45f, 35f },
            2 => new[] { 0f, 15f, 30f, 35f, 20f },
            3 => new[] { 10f, 20f, 30f, 25f, 15f },
            4 => new[] { 15f, 25f, 30f, 20f, 10f },
            _ => new[] { 20f, 25f, 30f, 15f, 10f }
        };
        string[] names = { "Убитое", "Плохое", "Среднее", "Хорошее", "Отличное" };
        for (int i = 0; i < chances.Length; i++)
        {
            roll -= chances[i];
            if (roll < 0f) return names[i];
        }
        return "Отличное";
    }

    private void RollConditionPercent()
    {
        conditionPercent = conditionName switch
        {
            "Отличное" => Random.Range(90, 101),
            "Хорошее" => Random.Range(70, 90),
            "Среднее" => Random.Range(50, 70),
            "Плохое" => Random.Range(25, 50),
            _ => Random.Range(0, 25)
        };
    }

    private int CalculateBuyPrice()
    {
        float zonePriceFactor = segment switch
        {
            1 => 0.67f, 2 => 0.68f, 3 => 0.72f, 4 => 0.71f, _ => 0.70f
        };
        float statePriceFactor = conditionName switch
        {
            "Убитое" => 0.70f, "Плохое" => 0.85f, "Хорошее" => 1.12f,
            "Отличное" => 1.25f, _ => 1.00f
        };
        float value = data.baseSellPrice * zonePriceFactor * statePriceFactor * Random.Range(0.97f, 1.03f);
        return PlayerData.RoundPrice(value);
    }

    private void GenerateRandomDefects(int count)
    {
        // Стоимость каждого присутствующего дефекта = доля узла × R(зоны) × S.
        List<DefectInfo> pool = new List<DefectInfo>()
        {
            new DefectInfo { type = DefectType.Body,       defectName = "Кузов",      baseRepairCost = GetNodeRepairCost(BodyRepairWeight),       sellBonus = 0.08f },
            new DefectInfo { type = DefectType.Engine,     defectName = "Двигатель",  baseRepairCost = GetNodeRepairCost(EngineRepairWeight),     sellBonus = 0.12f },
            new DefectInfo { type = DefectType.Suspension, defectName = "Подвеска",   baseRepairCost = GetNodeRepairCost(SuspensionRepairWeight), sellBonus = 0.06f },
            new DefectInfo { type = DefectType.Wheels,     defectName = "Колёса",     baseRepairCost = GetNodeRepairCost(WheelsRepairWeight),     sellBonus = 0.05f },
            new DefectInfo { type = DefectType.Interior,   defectName = "Салон",      baseRepairCost = GetNodeRepairCost(InteriorRepairWeight),   sellBonus = 0.03f }
        };

        // Перемешиваем пул случайным образом
        for (int i = 0; i < pool.Count; i++)
        {
            int rnd = Random.Range(i, pool.Count);
            (pool[i], pool[rnd]) = (pool[rnd], pool[i]);
        }

        count = Mathf.Min(count, pool.Count);
        List<DefectInfo> present = pool.GetRange(0, count);

        defects.Clear();
        defects.AddRange(present);
    }

    private int GetNodeRepairCost(int nodeWeight)
    {
        float rate = ZoneRepairRates[Mathf.Clamp(segment, 1, 5)];
        float value = data.baseSellPrice * rate * nodeWeight / TotalRepairWeight;
        return PlayerData.RoundPrice(value);
    }

    // Видимость дефектов (BALANCE.md, раздел 4 для уровня 0 и раздел 5.1 для уровней 1-4).
    public string GetFormattedDefectsString(int diagnostLevel)
    {
        if (defects.Count == 0)
            return "Дефекты: отсутствуют";

        int visibleLimit = diagnostLevel switch
        {
            // Без «Диагноста» показываем лимит, заданный состоянием объявления.
            0 => conditionName == "Отличное" ? 0 : conditionName == "Хорошее" ? 1
                 : conditionName == "Убитое" ? 3 : 2,
            // Уровни Диагноста (раздел 5.1): 1 -> 2, 2 -> 4, 3 и 4 -> все дефекты.
            1 => 2,
            2 => 4,
            3 => 5,
            4 => 5,
            _ => 0
        };

        int countToShow = Mathf.Min(visibleLimit, defects.Count);
        if (countToShow <= 0)
            return "Дефекты: не обнаружены";

        List<string> visibleNames = new List<string>();
        for (int i = 0; i < countToShow; i++)
        {
            visibleNames.Add(defects[i].defectName);
        }

        return "Дефекты: " + string.Join(", ", visibleNames);
    }
}
