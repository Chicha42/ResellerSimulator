using UnityEngine;
using YG;
using System;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    public List<CarData> allCarsCatalog = new List<CarData>();

    private const string LOCAL_KEY = "wheeldealer_save";
    private const float AUTO_SAVE_INTERVAL = 120f;
    private float autoSaveTimer = 0f;

    private bool _loaded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        YG2.onGetSDKData += OnSDKReady;
    }

    private void OnSDKReady()
    {
        YG2.onGetSDKData -= OnSDKReady; 
        LoadOnStart();
        _loaded = true;
    }

    private void Update()
    {
        autoSaveTimer += Time.unscaledDeltaTime;
        if (autoSaveTimer >= AUTO_SAVE_INTERVAL)
        {
            autoSaveTimer = 0f;
            Debug.Log("[SAVE] Автосохранение раз в 2 минуты");
            SaveAll();
        }
    }

    private void OnApplicationQuit() => SaveAll();
    private void OnApplicationPause(bool pause) { if (pause) SaveAll(); }


    public void SaveAll()
    {
        SaveData data = CollectFromPlayerData();
        string json = JsonUtility.ToJson(data);
        Debug.Log("[SAVE] JSON:\n" + json);

        // 1. Локально (LocalStorage)
        PlayerPrefs.SetString(LOCAL_KEY, json);
        PlayerPrefs.Save();

        // 2. В облако YG2 
        YG2.SetState("cloud_money", data.money);
        YG2.SetState("cloud_sales", data.totalSalesCount);
        YG2.SetState("cloud_diag", data.diagnostLevel);
        YG2.SetState("cloud_ware", data.warehouseLevel);
        YG2.SetState("cloud_garage", data.garageLevel);
        YG2.SetState("cloud_trend_x100", Mathf.RoundToInt(data.marketTrend * 100f));
        YG2.SetState("cloud_tick", data.actionTick);
        YG2.SetState("cloud_haggle_tick", data.lastHaggleSessionTick);


        YG2.SaveProgress();
        Debug.Log("[SAVE] Сохранено локально + ключевые числа в облако");
    }

    private SaveData CollectFromPlayerData()
    {
        var p = PlayerData.Instance;
        SaveData data = new SaveData
        {
            money = p.money,
            totalSalesCount = p.totalSalesCount,
            diagnostLevel = p.diagnostLevel,
            warehouseLevel = p.warehouseLevel,
            garageLevel = p.garageLevel,
            marketTrend = p.marketTrend,
            actionTick = p.actionTick,
            lastHaggleSessionTick = p.lastHaggleSessionTick
        };

        foreach (var car in p.garage)
        {
            CarSave cs = new CarSave
            {
                carDataId = car.data.id,
                segment = car.segment,
                conditionPercent = car.conditionPercent,
                conditionName = car.conditionName,
                buyPrice = car.buyPrice,
                totalRepairPaid = car.totalRepairPaid,
                defects = new List<DefectSave>()
            };

            foreach (var d in car.defects)
            {
                cs.defects.Add(new DefectSave
                {
                    type = (int)d.type,
                    baseRepairCost = d.baseRepairCost,
                    sellBonus = d.sellBonus,
                    isFixed = d.isFixed
                });
            }

            data.garage.Add(cs);
        }

        return data;
    }


    public void LoadOnStart()
    {
        // Шаг 1. Облако
        int cloudMoney = YG2.GetState("cloud_money");
        int cloudSales = YG2.GetState("cloud_sales");
        int cloudDiag = YG2.GetState("cloud_diag");
        int cloudWare = YG2.GetState("cloud_ware");
        int cloudGarage = YG2.GetState("cloud_garage");
        int cloudTrend100 = YG2.GetState("cloud_trend_x100");
        int cloudTick = YG2.GetState("cloud_tick");
        int cloudHaggleTick = YG2.GetState("cloud_haggle_tick");

        // Если в облаке что-то есть (money > 0) — берём облако
        if (cloudMoney > 0)
        {
            Debug.Log("[LOAD] Данные из ОБЛАКА");

            var p = PlayerData.Instance;
            p.money = cloudMoney;
            p.totalSalesCount = cloudSales;
            p.diagnostLevel = cloudDiag;
            p.warehouseLevel = cloudWare;
            p.garageLevel = cloudGarage;
            p.marketTrend = cloudTrend100 > 0 ? cloudTrend100 / 100f : 1f;
            p.actionTick = cloudTick;
            p.lastHaggleSessionTick = cloudHaggleTick == 0 ? -999 : cloudHaggleTick;

            // Купленные машины попробуем восстановить из локального JSON
            // (в облаке хранятся только числа)
            LoadGarageFromLocal();

            UIManager.Instance?.UpdateTopBar();
            GarageManager.Instance?.RefreshGarage();
            return;
        }

        // Шаг 2. LocalStorage
        if (PlayerPrefs.HasKey(LOCAL_KEY))
        {
            string json = PlayerPrefs.GetString(LOCAL_KEY);
            Debug.Log("[LOAD] Данные из ЛОКАЛА:\n" + json);
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(json);
                ApplyToPlayerData(data);
            }
            catch (Exception e)
            {
                Debug.LogError("[LOAD] Битый JSON: " + e.Message);
            }
            return;
        }

        Debug.Log("[LOAD] Новый игрок — данные по умолчанию");
    }

    private void LoadGarageFromLocal()
    {
        if (!PlayerPrefs.HasKey(LOCAL_KEY)) return;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(LOCAL_KEY));
            RebuildGarage(data.garage);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LOAD] Не удалось восстановить гараж: " + e.Message);
        }
    }

    private void ApplyToPlayerData(SaveData data)
    {
        var p = PlayerData.Instance;
        p.money = data.money;
        p.totalSalesCount = data.totalSalesCount;
        p.diagnostLevel = data.diagnostLevel;
        p.warehouseLevel = data.warehouseLevel;
        p.garageLevel = data.garageLevel;
        p.marketTrend = data.marketTrend;
        p.actionTick = data.actionTick;
        p.lastHaggleSessionTick = data.lastHaggleSessionTick;

        RebuildGarage(data.garage);

        UIManager.Instance?.UpdateTopBar();
        GarageManager.Instance?.RefreshGarage();
    }

    private void RebuildGarage(List<CarSave> saved)
    {
        var p = PlayerData.Instance;
        p.garage.Clear();

        foreach (var cs in saved)
        {
            // Ищем CarData в каталоге по id
            CarData data = allCarsCatalog.Find(c => c != null && c.id == cs.carDataId);
            if (data == null)
            {
                Debug.LogWarning($"[LOAD] Машина с id={cs.carDataId} не найдена в каталоге — пропущена");
                continue;
            }

            // Создаём "пустой" инстанс и вручную перезаписываем его поля
            CarInstance car = new CarInstance(data, cs.segment);
            car.conditionPercent = cs.conditionPercent;
            car.conditionName = cs.conditionName;
            car.buyPrice = cs.buyPrice;
            car.totalRepairPaid = cs.totalRepairPaid;

            car.defects.Clear();
            foreach (var ds in cs.defects)
            {
                car.defects.Add(new DefectInfo
                {
                    type = (DefectType)ds.type,
                    defectName = GetDefectName((DefectType)ds.type),
                    baseRepairCost = ds.baseRepairCost,
                    sellBonus = ds.sellBonus,
                    isFixed = ds.isFixed
                });
            }

            p.garage.Add(car);
        }

        Debug.Log($"[LOAD] Восстановлено машин: {p.garage.Count}");
    }

    private string GetDefectName(DefectType type)
    {
        return type switch
        {
            DefectType.Body => "Кузов",
            DefectType.Engine => "Двигатель",
            DefectType.Suspension => "Подвеска",
            DefectType.Wheels => "Колёса",
            DefectType.Interior => "Салон",
            _ => "Дефект"
        };
    }


    public void WipeAll()
    {
        // 1. Локально
        PlayerPrefs.DeleteKey(LOCAL_KEY);
        PlayerPrefs.Save();

        // 2. Облако YG2
        YG2.SetState("cloud_money", 0);
        YG2.SetState("cloud_sales", 0);
        YG2.SetState("cloud_diag", 0);
        YG2.SetState("cloud_ware", 0);
        YG2.SetState("cloud_garage", 0);
        YG2.SetState("cloud_trend_x100", 0);
        YG2.SetState("cloud_tick", 0);
        YG2.SetState("cloud_haggle_tick", 0);
        YG2.SaveProgress();

        // 3. Сбрасываем данные игрока в исходное состояние
        var p = PlayerData.Instance;
        p.money = 25000;
        p.totalSalesCount = 0;
        p.diagnostLevel = 0;
        p.warehouseLevel = 0;
        p.garageLevel = 0;
        p.marketTrend = 1f;
        p.actionTick = 0;
        p.lastHaggleSessionTick = -999;
        p.garage.Clear();

        // 4. Обновляем интерфейс
        UIManager.Instance?.UpdateTopBar();
        GarageManager.Instance?.RefreshGarage();

        Debug.Log("[SAVE] Прогресс полностью сброшен");
    }
}