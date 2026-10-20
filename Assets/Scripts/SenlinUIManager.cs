using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Senlin Scene UI Manager - Manages all UI panels in the senlin (forest) scene
/// Independent of the cabin scene's UIManager, both scenes have their own dedicated UIManager
/// </summary>
public class SenlinUIManager : MonoBehaviour
{
    public static SenlinUIManager instance;

    [Header("UI Panel References")]
    public GameObject mainMenuPanel;
    public GameObject inventoryPanel;
    public GameObject orderPanel;
    public GameObject skillBarPanel;
    public GameObject clueDiaryPanel;
    public GameObject dialogPanel;

    [Header("Character Name")]
    public string playerName = "Abigail";
    public TextMeshProUGUI playerNameText;

    [Header("Interaction Prompt")]
    public GameObject interactPrompt;
    public TextMeshProUGUI interactPromptText;

    [Header("Hotkey Bindings")]
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode diaryKey = KeyCode.J;
    public KeyCode orderKey = KeyCode.O;
    public KeyCode menuKey = KeyCode.Escape;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SenlinUIManager] Scene loaded: {scene.name}, re-finding UI panels...");
        FindUIPanels();
        CloseAllPanels();
    }

    void Start()
    {
        Invoke("InitializeUI", 0.1f);
    }

    void InitializeUI()
    {
        FindUIPanels();
        CloseAllPanels();

        if (playerNameText != null)
            playerNameText.text = playerName;

        Debug.Log("[SenlinUIManager] UI initialization complete");
    }

    void FindUIPanels()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform uiRoot = canvas.transform.Find("UIRoot");
            if (uiRoot != null)
            {
                if (inventoryPanel == null)
                    inventoryPanel = uiRoot.Find("InventoryPanel")?.gameObject;
                if (clueDiaryPanel == null)
                    clueDiaryPanel = uiRoot.Find("ClueDiaryPanel")?.gameObject;
                if (orderPanel == null)
                    orderPanel = uiRoot.Find("OrderPanel")?.gameObject;
                if (skillBarPanel == null)
                    skillBarPanel = uiRoot.Find("SkillBarPanel")?.gameObject;
                if (dialogPanel == null)
                    dialogPanel = uiRoot.Find("DialogPanel")?.gameObject;
                if (interactPrompt == null)
                    interactPrompt = uiRoot.Find("InteractPrompt")?.gameObject;

                if (playerNameText == null)
                {
                    Transform playerNameTrans = uiRoot.Find("PlayerName");
                    if (playerNameTrans != null)
                        playerNameText = playerNameTrans.GetComponent<TextMeshProUGUI>();
                }

                if (interactPromptText == null && interactPrompt != null)
                {
                    interactPromptText = interactPrompt.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
        }

        Debug.Log($"[SenlinUIManager] Panel references: inventory={inventoryPanel != null}, diary={clueDiaryPanel != null}, order={orderPanel != null}");
    }

    void Update()
    {
        HandleHotkeys();
        UpdateCursorState();
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            Debug.Log("[SenlinUIManager] I key pressed");
            ToggleInventory();
        }

        if (Input.GetKeyDown(diaryKey))
        {
            Debug.Log("[SenlinUIManager] J key pressed");
            ToggleClueDiary();
        }

        if (Input.GetKeyDown(orderKey))
        {
            Debug.Log("[SenlinUIManager] O key pressed");
            ToggleOrderPanel();
        }

        if (Input.GetKeyDown(menuKey))
        {
            Debug.Log("[SenlinUIManager] Escape key pressed");
            if (mainMenuPanel != null)
            {
                bool wasActive = mainMenuPanel.activeSelf;
                mainMenuPanel.SetActive(!wasActive);
            }
        }
    }

    void UpdateCursorState()
    {
        bool isUIOpen = (inventoryPanel != null && inventoryPanel.activeSelf) ||
                        (clueDiaryPanel != null && clueDiaryPanel.activeSelf) ||
                        (orderPanel != null && orderPanel.activeSelf);

        GameObject mainMenu = GameObject.Find("MainMenuPanel");
        bool isMainMenuOpen = mainMenu != null && mainMenu.activeSelf;

        Cursor.visible = isUIOpen || isMainMenuOpen;
        Cursor.lockState = (isUIOpen || isMainMenuOpen) ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void CloseAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (orderPanel != null) orderPanel.SetActive(false);
        if (skillBarPanel != null) skillBarPanel.SetActive(false);
        if (clueDiaryPanel != null) clueDiaryPanel.SetActive(false);
        if (dialogPanel != null) dialogPanel.SetActive(false);
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool wasActive = inventoryPanel.activeSelf;
            inventoryPanel.SetActive(!wasActive);
            if (!wasActive) CloseOtherPanels(inventoryPanel);
        }
        else
        {
            Debug.LogWarning("[SenlinUIManager] inventoryPanel reference is null!");
        }
    }

    public void ToggleClueDiary()
    {
        if (clueDiaryPanel != null)
        {
            bool wasActive = clueDiaryPanel.activeSelf;
            clueDiaryPanel.SetActive(!wasActive);
            if (!wasActive)
            {
                CloseOtherPanels(clueDiaryPanel);
                ClueDiarySystem.instance?.UpdatePageDisplay();
            }
        }
        else
        {
            Debug.LogWarning("[SenlinUIManager] clueDiaryPanel reference is null!");
        }
    }

    public void ToggleOrderPanel()
    {
        if (orderPanel != null)
        {
            bool wasActive = orderPanel.activeSelf;
            orderPanel.SetActive(!wasActive);
            if (!wasActive) CloseOtherPanels(orderPanel);
        }
        else
        {
            Debug.LogWarning("[SenlinUIManager] orderPanel reference is null!");
        }
    }

    public void ToggleMainMenu()
    {
        if (mainMenuPanel != null)
        {
            bool wasActive = mainMenuPanel.activeSelf;
            mainMenuPanel.SetActive(!wasActive);
            if (!wasActive) CloseOtherPanels(mainMenuPanel);
        }
        else
        {
            Debug.LogWarning("[SenlinUIManager] mainMenuPanel reference is null!");
        }
    }

    public void ShowInteractPrompt(string message)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(true);
            if (interactPromptText != null)
                interactPromptText.text = message;
        }
    }

    public void HideInteractPrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void CloseOtherPanels(GameObject except)
    {
        if (except != mainMenuPanel && mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (except != inventoryPanel && inventoryPanel != null) inventoryPanel.SetActive(false);
        if (except != orderPanel && orderPanel != null) orderPanel.SetActive(false);
        if (except != clueDiaryPanel && clueDiaryPanel != null) clueDiaryPanel.SetActive(false);
    }

    public void UnlockSkillBar()
    {
        if (skillBarPanel != null)
            skillBarPanel.SetActive(true);
    }
}