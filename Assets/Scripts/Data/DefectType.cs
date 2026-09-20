using System;

public enum DefectType
{
    Body, // Кузов (+8% к продаже, база ремонта 5 000)
    Engine, // Двигатель (+12%, база ремонта 8 000) 
    Suspension, // Подвеска (+6%, база ремонта 4 000)
    Wheels, // Колёса (+5%, база ремонта 3 000)
    Interior // Салон (+3%, база ремонта 2 000)
}

[Serializable]
public class DefectInfo
{
    public DefectType type;
    public string defectName;
    public int baseRepairCost;
    public float sellBonus;
    public bool isFixed;
}