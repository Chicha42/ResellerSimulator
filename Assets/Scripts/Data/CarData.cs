using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "CarDealer/Car Data")]
public class CarData : ScriptableObject
{
    [Header("Базовая информация")]
    public int id;
    public string carName;
    public int requiredSalesToUnlock; // Сколько продаж нужно для открытия (0, 2, 4...)

    [Header("Экономика")]
    public int baseBuyPrice;   // База сегмента из BALANCE.md
    public int baseSellPrice;  // Legacy: в формулах раздела 4 не используется
    public float repairMultiplier = 1f; // Legacy: ремонт зависит от состояния машины
}
