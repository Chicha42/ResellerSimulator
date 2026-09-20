using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "CarDealer/Car Data")]
public class CarData : ScriptableObject
{
    [Header("Базовая информация")]
    public int id;
    public string carName;
    public int requiredSalesToUnlock; // Сколько продаж нужно для открытия (0, 2, 4...)

    [Header("Экономика")]
    public int baseBuyPrice;   // Базовая цена покупки продавца
    public int baseSellPrice;  // Базовая цена продажи
    public float repairMultiplier = 1f; // Множитель ремонта (x1 для дешевых, x2, x3... для дорогих)
}