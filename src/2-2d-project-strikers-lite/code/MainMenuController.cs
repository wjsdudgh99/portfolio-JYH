using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Button Reference")]
    public Button StartButton;
    public Button ExitButton;

    private void Start()
    {
        if (StartButton != null)
        {
            StartButton.onClick.AddListener(() => PlayGame());
        }

        if (ExitButton != null)
        {
            ExitButton.onClick.AddListener(() => QuitGame());
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("InGame");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
