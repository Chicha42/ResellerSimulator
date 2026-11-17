using System.Collections.Generic;

[System.Serializable]
public class DefectSave
{
    public int type;           
    public int baseRepairCost;
    public float sellBonus;
    public bool isFixed;
}

[System.Serializable]
public class CarSave
{
    public int carDataId;      
    public int segment;
    public int conditionPercent;
    public string conditionName;
    public int buyPrice;
    public int totalRepairPaid;
    public List<DefectSave> defects = new List<DefectSave>();
}

[System.Serializable]
public class SaveData
{
    public int money;
    public int totalSalesCount;
    public int diagnostLevel;
    public int warehouseLevel;
    public int garageLevel;
    public float marketTrend;
    public int actionTick;
    public int lastHaggleSessionTick;
    public List<CarSave> garage = new List<CarSave>();
}