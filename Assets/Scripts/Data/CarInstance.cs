using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CarInstance
{
    private const float RepairCostMultiplier = 1.55f;
    private const float FullRepairReferenceCost = 22000f;

    public CarData data;
    public int segment = 1;

    public int conditionPercent; // 0-100%
    public string conditionName;  // "Отличное", "Хорошее", "Среднее", "Плохое", "Убитое"
    public int buyPrice;          // Итоговая цена покупки с разбросом

    public List<DefectInfo> defects = new List<DefectInfo>();
    public int totalRepairPaid = 0;

    public CarInstance(CarData carData, int carSegment = 1)
    {
        data = carData;
        segment = carSegment;
        GenerateCar();
    }

    private void GenerateCar()
    {
        // 1. Диапазон состояния по сегментам (Раздел 3.1)
        if (segment <= 3)
            conditionPercent = Random.Range(40, 101); // Дешёвые: 40-100% (без "Убитого")
        else if (segment <= 5)
            conditionPercent = Random.Range(20, 101); // Средние: 20-100%
        else
            conditionPercent = Random.Range(0, 101);  // Дорогие: 0-100%

        // 2. Параметры состояния (Раздел 3)
        float buyMult;
        float repairMult;
        int dMin, dMax;

        if (conditionPercent >= 90)      { conditionName = "Отличное"; buyMult = 0.95f; repairMult = 0.2f; dMin = 0; dMax = 1; }
        else if (conditionPercent >= 70) { conditionName = "Хорошее";  buyMult = 0.85f; repairMult = 0.6f; dMin = 1; dMax = 2; }
        else if (conditionPercent >= 50) { conditionName = "Среднее";  buyMult = 0.75f; repairMult = 1.0f; dMin = 2; dMax = 3; }
        else if (conditionPercent >= 25) { conditionName = "Плохое";   buyMult = 0.65f; repairMult = 1.3f; dMin = 3; dMax = 4; }
        else                             { conditionName = "Убитое";   buyMult = 0.55f; repairMult = 1.6f; dMin = 4; dMax = 5; }

        // 3. Цена покупки с разбросом rand(0.9, 1.1)
        float priceRand = Random.Range(0.9f, 1.1f);
        buyPrice = Mathf.RoundToInt(data.baseBuyPrice * buyMult * priceRand);

        // 4. Генерация дефектов
        int defectCount = Random.Range(dMin, dMax + 1);
        GenerateRandomDefects(defectCount, repairMult);
    }

    private void GenerateRandomDefects(int count, float repairMult)
    {
        // Пул всех 5 возможных дефектов
        List<DefectInfo> pool = new List<DefectInfo>()
        {
            new DefectInfo { type = DefectType.Body, defectName = "Кузов", baseRepairCost = 5000, sellBonus = 0.08f },
            new DefectInfo { type = DefectType.Engine, defectName = "Двигатель", baseRepairCost = 8000, sellBonus = 0.12f },
            new DefectInfo { type = DefectType.Suspension, defectName = "Подвеска", baseRepairCost = 4000, sellBonus = 0.06f },
            new DefectInfo { type = DefectType.Wheels, defectName = "Колёса", baseRepairCost = 3000, sellBonus = 0.05f },
            new DefectInfo { type = DefectType.Interior, defectName = "Салон", baseRepairCost = 2000, sellBonus = 0.03f }
        };

        // Перемешиваем пул случайным образом
        for (int i = 0; i < pool.Count; i++)
        {
            int rnd = Random.Range(i, pool.Count);
            (pool[i], pool[rnd]) = (pool[rnd], pool[i]);
        }

        // Берём нужное количество дефектов
        count = Mathf.Min(count, pool.Count);
        defects.Clear();

        int fullRepairCost = 0;
        for (int i = 0; i < count; i++)
        {
            fullRepairCost += pool[i].baseRepairCost;
        }

        for (int i = 0; i < count; i++)
        {
            var def = pool[i];
            // Формула из раздела 4: База дефекта × (Полный ремонт / 22 000) × Множ. ремонта × 1.55 × rand(0.8, 1.3)
            float costRand = Random.Range(0.8f, 1.3f);
            float fullRepairRatio = fullRepairCost / FullRepairReferenceCost;
            int finalCost = Mathf.RoundToInt(def.baseRepairCost * fullRepairRatio * repairMult * RepairCostMultiplier * costRand);

            def.baseRepairCost = finalCost;
            defects.Add(def);
        }
    }

    public string GetFormattedDefectsString(int diagnostLevel)
    {
        if (defects.Count == 0)
            return "Дефекты: отсутствуют";

        int visibleLimit = diagnostLevel switch
        {
            0 => Random.Range(1, 3), // Видно 1-2 дефекта
            1 => 2,
            2 => 4,
            _ => defects.Count       // 3 и 4 уровень — видно всё
        };

        List<string> visibleNames = new List<string>();
        int countToShow = Mathf.Min(visibleLimit, defects.Count);

        for (int i = 0; i < countToShow; i++)
        {
            visibleNames.Add(defects[i].defectName);
        }

        return "Дефекты: " + string.Join(", ", visibleNames);
    }
}
