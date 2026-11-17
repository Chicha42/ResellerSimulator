using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Начать новую игру (пока без изменения сохранений)
    public void NewGame()
    {
        SceneManager.LoadScene("Gameplay");
    }

    // Выйти из игры
    public void ExitGame()
    {
        Debug.Log("Выход из игры");

        Application.Quit();
    }
}