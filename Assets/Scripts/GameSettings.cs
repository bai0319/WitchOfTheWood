using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSettings : MonoBehaviour
{
    public static GameSettings instance;

    public GameObject settingsPanel;

    public Button continueButton;
    public Button exitToMenuButton;
    public Button exitGameButton;

    public TextMeshProUGUI controlsText;

    private bool isSettingsOpen = false;

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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (MainMenu.instance != null && MainMenu.instance.mainMenuPanel != null && MainMenu.instance.mainMenuPanel.activeSelf)
            {
                return;
            }

            if (isSettingsOpen)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }

        isSettingsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        isSettingsOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
    }

    public void ContinueGame()
    {
        CloseSettings();
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        isSettingsOpen = false;

        if (MainMenu.instance != null)
        {
            MainMenu.instance.ShowMainMenu();
        }
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        Application.Quit();
#endif
    }
}