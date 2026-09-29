using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CarInstance
{
    // ── BALANCE.md, раздел 4: базовые стоимости 5 узлов. Сумма = 22 000 ₽.
    // Используются как ВЕСА, по которым раскладывается полный ремонт машины (repairMin..repairMax)
    // на конкретные дефекты этой машины. Менять здесь — менять доли узлов в стоимости ремонта.
    private const int BodyBaseCost = 5000;
    private const int EngineBaseCost = 8000;
    private const int SuspensionBaseCost = 4000;
    private const int WheelsBaseCost = 3000;
    private const int InteriorBaseCost = 2000;

    public CarData data;
    public int segment = 1;

    public int conditionPercent; // 0-100% (BALANCE.md, раздел 4 — пороги 90/70/50/25)
    public string conditionName; // "Отличное", "Хорошее", "Среднее", "Плохое", "Убитое"
    public int buyPrice;         // Итоговая цена покупки (BALANCE.md, раздел 3, колонка «Покупка»)

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
        // ── 1. Состояние по зонам. BALANCE.md НЕ задаёт распределение состояния —
        //    это значения прототипа: чем дороже зона, тем выше шанс убитой машины.
        //    Меняй границы здесь, если понадобится другой разброс.
        switch (segment)
        {
            case 1:  conditionPercent = Random.Range(40, 101); break; // Зона 1: 40-100% (без «Убитого»)
            case 2:  conditionPercent = Random.Range(30, 101); break; // Зона 2: 30-100%
            case 3:  conditionPercent = Random.Range(20, 101); break; // Зона 3: 20-100%
            case 4:  conditionPercent = Random.Range(10, 101); break; // Зона 4: 10-100%
            default: conditionPercent = Random.Range(0, 101);  break; // Зона 5: 0-100%
        }

        // ── 2. Имя состояния по BALANCE.md, раздел 4 (пороги 90/70/50/25).
        //    Число дефектов (dMin..dMax) в балансе не задано — значения прототипа.
        //    Важно: минимум 1 дефект, иначе полный ремонт = 0 и прибыль превысит «Лучший» из таблицы.
        int dMin, dMax;
        if (conditionPercent >= 90)      { conditionName = "Отличное"; dMin = 1; dMax = 1; }
        else if (conditionPercent >= 70) { conditionName = "Хорошее";  dMin = 1; dMax = 2; }
        else if (conditionPercent >= 50) { conditionName = "Среднее";  dMin = 2; dMax = 3; }
        else if (conditionPercent >= 25) { conditionName = "Плохое";   dMin = 3; dMax = 4; }
        else                             { conditionName = "Убитое";   dMin = 4; dMax = 5; }

        // ── 3. Цена покупки (BALANCE.md, раздел 1: «Цена покупки = цена продавца × (1 − скидка торга)»).
        //    Торг при покупке (раздел 6) пока не реализован, поэтому скидка = 0
        //    и цена покупки равна табличной колонке «Покупка».
        buyPrice = data.baseBuyPrice;

        // ── 4. Генерация дефектов
        int defectCount = Random.Range(dMin, dMax + 1);
        GenerateRandomDefects(defectCount);
    }

    private void GenerateRandomDefects(int count)
    {
        // Пул всех 5 возможных дефектов (BALANCE.md, раздел 4).
        // baseRepairCost здесь — БАЗОВЫЙ вес узла (5 000 / 8 000 / 4 000 / 3 000 / 2 000),
        // sellBonus — вес влияния на продажу (8% / 12% / 6% / 5% / 3%, раздел 4).
        List<DefectInfo> pool = new List<DefectInfo>()
        {
            new DefectInfo { type = DefectType.Body,       defectName = "Кузов",      baseRepairCost = BodyBaseCost,       sellBonus = 0.08f },
            new DefectInfo { type = DefectType.Engine,     defectName = "Двигатель",  baseRepairCost = EngineBaseCost,     sellBonus = 0.12f },
            new DefectInfo { type = DefectType.Suspension, defectName = "Подвеска",   baseRepairCost = SuspensionBaseCost, sellBonus = 0.06f },
            new DefectInfo { type = DefectType.Wheels,     defectName = "Колёса",     baseRepairCost = WheelsBaseCost,     sellBonus = 0.05f },
            new DefectInfo { type = DefectType.Interior,   defectName = "Салон",      baseRepairCost = InteriorBaseCost,   sellBonus = 0.03f }
        };

        // Перемешиваем пул случайным образом
        for (int i = 0; i < pool.Count; i++)
        {
            int rnd = Random.Range(i, pool.Count);
            (pool[i], pool[rnd]) = (pool[rnd], pool[i]);
        }

        count = Mathf.Min(count, pool.Count);
        List<DefectInfo> present = pool.GetRange(0, count);

        // ── BALANCE.md, раздел 3 (таблица 3.1): полный ремонт машины стоит случайную сумму
        //    в вилке [repairMin .. repairMax]. Эта сумма — источник истины, её и раскладываем.
        int targetRepair = Random.Range(data.repairMin, data.repairMax + 1);

        int weightSum = 0;
        foreach (var d in present) weightSum += d.baseRepairCost;

        defects.Clear();
        int distributed = 0;
        for (int i = 0; i < present.Count; i++)
        {
            int cost;
            if (i == present.Count - 1)
                cost = Mathf.Max(0, targetRepair - distributed); // остаток отдаём последнему узлу — сумма точная
            else
                cost = Mathf.RoundToInt((float)targetRepair * present[i].baseRepairCost / weightSum);

            distributed += cost;
            present[i].baseRepairCost = cost; // итоговая база ремонта ЭТОЙ машины (до скидки склада)
            defects.Add(present[i]);
        }
    }

    // Видимость дефектов (BALANCE.md, раздел 4 для уровня 0 и раздел 5.1 для уровней 1-4).
    public string GetFormattedDefectsString(int diagnostLevel)
    {
        if (defects.Count == 0)
            return "Дефекты: отсутствуют";

        int visibleLimit = diagnostLevel switch
        {
            // Без «Диагноста» видно не 1-2 случайных, а зависит от состояния машины (раздел 4):
            // 90-100% -> 0, 70-89% -> 1, 50-69% -> 2, 25-49% -> 2, 0-24% -> 3.
            0 => conditionPercent >= 90 ? 0
                 : conditionPercent >= 70 ? 1
                 : conditionPercent >= 50 ? 2
                 : conditionPercent >= 25 ? 2
                 : 3,
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
