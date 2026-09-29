using UnityEngine;
using YG;

public class SaveTester : MonoBehaviour
{
    public void AddCoins()
    {
        SaveManager.Data.coins += 100;
        Debug.Log($"[TEST] Монет теперь: {SaveManager.Data.coins}");
    }

    public void NextLevel()
    {
        SaveManager.Data.level += 1;
        Debug.Log($"[TEST] Уровень теперь: {SaveManager.Data.level}");
    }

    public void ToggleSound()
    {
        SaveManager.Data.soundOn = !SaveManager.Data.soundOn;
        Debug.Log($"[TEST] Звук: {SaveManager.Data.soundOn}");
    }

    public void ForceSave()
    {
        SaveManager.Instance.SaveAll();
        Debug.Log("[TEST] Принудительное сохранение выполнено");
    }

    public void ForceLoad()
    {
        SaveManager.Instance.LoadOnStart();
        Debug.Log($"[TEST] После загрузки: level={SaveManager.Data.level}, coins={SaveManager.Data.coins}");
    }

    public void WipeSave()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        YG2.SetState("cloud_level", 0);
        YG2.SetState("cloud_coins", 0);
        YG2.SaveProgress();

        SaveManager.Data = new SaveData();

        Debug.Log("[TEST] Всё стёрто (локально + облако)");
    }
}