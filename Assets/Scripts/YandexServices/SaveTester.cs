using UnityEngine;

public class SaveTester : MonoBehaviour
{
    public void ForceSave()
    {
        SaveManager.Instance.SaveAll();
        Debug.Log("[TEST] Принудительное сохранение выполнено");
    }

    public void WipeSave()
    {
        SaveManager.Instance.WipeAll();
        Debug.Log("[TEST] Кнопка сброса нажата");
    }
}