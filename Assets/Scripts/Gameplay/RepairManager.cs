using System.Collections.Generic;
using UnityEngine;

/// <summary>Runs one repair per car; separate garage slots repair in parallel.</summary>
public sealed class RepairManager : MonoBehaviour
{
    public static RepairManager Instance { get; private set; }
    private sealed class RepairJob { public CarInstance car; public DefectInfo defect; }
    private readonly List<RepairJob> _active = new List<RepairJob>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var job = _active[i];
            var defect = job.defect;
            if (job.car == null || PlayerData.Instance == null || !PlayerData.Instance.garage.Contains(job.car))
            {
                defect.isRepairing = false;
                _active.RemoveAt(i);
                continue;
            }
            defect.repairSecondsRemaining = Mathf.Max(0f, defect.repairSecondsRemaining - dt);
            if (defect.repairSecondsRemaining > 0f) continue;

            defect.isRepairing = false;
            defect.isFixed = true;
            _active.RemoveAt(i);
            RetentionManager.Instance?.OnDefectsRepaired(1);
            if (GarageManager.Instance != null && GarageManager.Instance.carDetailsModal != null
                && GarageManager.Instance.carDetailsModal.CurrentCar == job.car)
                GarageManager.Instance.carDetailsModal.Refresh();
            GarageManager.Instance?.RefreshGarage();
            UIManager.Instance?.UpdateTopBar();
            SaveManager.Instance?.SaveAll();
        }

        GarageManager.Instance?.carDetailsModal?.RefreshRepairProgress();
    }

    public bool IsRepairing(CarInstance car) => car != null && car.defects.Exists(d => d.isRepairing);

    public bool StartRepair(CarInstance car, DefectInfo defect, int cost)
    {
        if (car == null || defect == null || defect.isFixed || defect.isRepairing || IsRepairing(car)
            || !PlayerData.Instance.garage.Contains(car) || cost < 0 || PlayerData.Instance.money < cost)
            return false;

        PlayerData.Instance.money -= cost;
        car.totalRepairPaid += cost;
        defect.repairDurationSeconds = RepairDuration(defect.type);
        defect.repairSecondsRemaining = defect.repairDurationSeconds;
        defect.isRepairing = true;
        _active.Add(new RepairJob { car = car, defect = defect });
        PlayerData.Instance.TickWorld();
        RetentionManager.Instance?.OnMoneyChanged();
        UIManager.Instance?.UpdateTopBar();
        SaveManager.Instance?.SaveAll();
        return true;
    }

    public bool CompleteImmediately(CarInstance car, DefectInfo defect)
    {
        if (car == null || defect == null || defect.isFixed || !defect.isRepairing) return false;
        _active.RemoveAll(job => job.car == car && job.defect == defect);
        defect.repairSecondsRemaining = 0f;
        defect.isRepairing = false;
        defect.isFixed = true;
        RetentionManager.Instance?.OnDefectsRepaired(1);
        GarageManager.Instance?.carDetailsModal?.Refresh();
        GarageManager.Instance?.RefreshGarage();
        SaveManager.Instance?.SaveAll();
        return true;
    }

    public void CancelForCar(CarInstance car)
    {
        if (car == null) return;
        foreach (var defect in car.defects)
        {
            if (!defect.isRepairing) continue;
            defect.isRepairing = false;
        }
        _active.RemoveAll(job => job.car == car);
    }

    public float Progress(DefectInfo defect) => defect == null || !defect.isRepairing || defect.repairDurationSeconds <= 0f
        ? (defect != null && defect.isFixed ? 1f : 0f)
        : Mathf.Clamp01(1f - defect.repairSecondsRemaining / defect.repairDurationSeconds);

    private static float RepairDuration(DefectType type) => type switch
    {
        DefectType.Body => 30f,
        DefectType.Engine => 45f,
        DefectType.Suspension => 25f,
        DefectType.Wheels => 20f,
        _ => 10f
    };
}
