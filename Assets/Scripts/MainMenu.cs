using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public static MainMenu instance;

    public GameObject mainMenuPanel;
    public Image backgroundImage;
    public Sprite defaultBackground;

    public Button startButton;
    public Button exitButton;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (UIManager.instance != null)
        {
            UIManager.instance.ShowGameUI();
        }

        Debug.Log("Game started!");
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UIManager.instance != null)
        {
            UIManager.instance.HideGameUI();
        }
    }

    public void SetBackground(Sprite sprite)
    {
        if (backgroundImage != null && sprite != null)
        {
            backgroundImage.sprite = sprite;
        }
    }
}