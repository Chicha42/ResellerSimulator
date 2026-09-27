using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "CarDealer/Car Data")]
public class CarData : ScriptableObject
{
    [Header("Базовая информация")]
    public int id;
    public string carName;
    public int requiredSalesToUnlock; // Колонка «Открывается» из BALANCE.md, раздел 3 (таблица 3.1)
    public int segment = 1;            // Зона машины 1..5 из BALANCE.md, раздел 3 (задаёт риск-пороги состояния)

    [Header("Экономика (BALANCE.md, раздел 3 — таблица 3.1)")]
    // Все четыре цифры берутся НАПРЯМУЮ из таблицы машин (раздел 3), ничего не считается по формулам.
    public int baseBuyPrice;   // Колонка «Покупка» — цена продавца (до торга, раздел 6)
    public int baseSellPrice;  // Колонка «Продажа» — цена полностью починенной машины при репутации ур.1 и нейтральном рынке
    public int repairMin;      // Колонка «Ремонт мин» — минимальная стоимость полного ремонта всех дефектов
    public int repairMax;      // Колонка «Ремонт макс» — максимальная стоимость полного ремонта всех дефектов
}
