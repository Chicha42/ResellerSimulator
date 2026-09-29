using UnityEngine;
using YG;
using System;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public static SaveData Data = new SaveData();

    private const string LOCAL_KEY = "save_json"; 
    private const string CLOUD_LEVEL = "cloud_level";
    private const string CLOUD_COINS = "cloud_coins";

    private float autoSaveTimer = 0f;
    private const float AUTO_SAVE_INTERVAL = 120f; 

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadOnStart(); 
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

    private void OnApplicationQuit()
    {
        Debug.Log("[SAVE] Игра закрывается — сохраняем");
        SaveAll();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveAll();
    }

    public void SaveAll()
    {
        string json = JsonUtility.ToJson(Data);
        Debug.Log("[SAVE] JSON для сохранения:\n" + json);

        PlayerPrefs.SetString(LOCAL_KEY, json);
        PlayerPrefs.Save();

        YG2.SetState(CLOUD_LEVEL, Data.level);
        YG2.SetState(CLOUD_COINS, Data.coins);
        YG2.SaveProgress();

        Debug.Log("[SAVE] Сохранено локально + в облако");
    }

    public void LoadOnStart()
    {
        int cloudLevel = YG2.GetState(CLOUD_LEVEL);
        int cloudCoins = YG2.GetState(CLOUD_COINS);

        if (cloudLevel > 0)
        {
            Debug.Log("[LOAD] Данные из ОБЛАКА: level=" + cloudLevel + ", coins=" + cloudCoins);
            Data.level = cloudLevel;
            Data.coins = cloudCoins;

            LoadSettingsFromLocal();
            return;
        }

        if (PlayerPrefs.HasKey(LOCAL_KEY))
        {
            string json = PlayerPrefs.GetString(LOCAL_KEY);
            Debug.Log("[LOAD] Данные из ЛОКАЛА:\n" + json);
            try
            {
                Data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[LOAD] Локальные данные успешно применены");
            }
            catch (Exception e)
            {
                Debug.LogError("[LOAD] Битый JSON, начинаем с нуля. " + e.Message);
                Data = new SaveData();
            }
            return;
        }

        Debug.Log("[LOAD] Сохранений нет — новый игрок");
        Data = new SaveData();
    }

    private void LoadSettingsFromLocal()
    {
        if (!PlayerPrefs.HasKey(LOCAL_KEY)) return;
        try
        {
            SaveData temp = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(LOCAL_KEY));
            Data.soundOn = temp.soundOn;
            Data.musicOn = temp.musicOn;
            Data.mouseSensitivity = temp.mouseSensitivity;
        }
        catch {}
    }
}