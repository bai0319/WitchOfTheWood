using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// UI管理器 - 管理所有UI面板的显示与隐藏
/// 羊皮纸风格UI，简约手绘边框
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("UI面板引用")]
    public GameObject mainMenuPanel;        // 主界面
    public GameObject inventoryPanel;       // 背包面板
    public GameObject orderPanel;           // 订单面板
    public GameObject skillBarPanel;        // 技能图标栏
    public GameObject clueDiaryPanel;       // 线索日记本
    public GameObject dialogPanel;          // 对话框

    [Header("角色名称")]
    public string playerName = "Abigail";
    public TextMeshProUGUI playerNameText;   // 头顶显示的角色名

    [Header("交互提示")]
    public GameObject interactPrompt;       // 交互提示UI
    public TextMeshProUGUI interactPromptText;

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
        Debug.Log($"[UIManager] Scene loaded: {scene.name}, re-finding UI panels...");
        InitializeUI();
    }

    void Start()
    {
        Invoke("InitializeUI", 0.1f);
    }

    void InitializeUI()
    {
        FindUIPanels();
        CloseAllPanels();

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (playerNameText != null)
            playerNameText.text = playerName;
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

                if (interactPromptText == null)
                {
                    Transform promptTextTrans = uiRoot.Find("InteractPrompt");
                    if (promptTextTrans != null)
                        interactPromptText = promptTextTrans.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        Debug.Log($"UIManager references: inventory={inventoryPanel != null}, diary={clueDiaryPanel != null}, order={orderPanel != null}");
    }

    /// <summary>
    /// 关闭所有UI面板
    /// </summary>
    public void CloseAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (orderPanel != null) orderPanel.SetActive(false);
        if (skillBarPanel != null) skillBarPanel.SetActive(false);
        if (clueDiaryPanel != null) clueDiaryPanel.SetActive(false);
        if (dialogPanel != null) dialogPanel.SetActive(false);
    }

    /// <summary>
    /// 切换背包面板
    /// </summary>
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
            Debug.LogWarning("UIManager: inventoryPanel reference is null!");
        }
    }

    /// <summary>
    /// 切换线索日记本
    /// </summary>
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
            Debug.LogWarning("UIManager: clueDiaryPanel reference is null!");
        }
    }

    /// <summary>
    /// 切换订单面板
    /// </summary>
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
            Debug.LogWarning("UIManager: orderPanel reference is null!");
        }
    }

    /// <summary>
    /// 切换主菜单面板
    /// </summary>
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
            Debug.LogWarning("UIManager: mainMenuPanel reference is null!");
        }
    }

    /// <summary>
    /// 显示交互提示
    /// </summary>
    public void ShowInteractPrompt(string message)
    {
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(true);
            if (interactPromptText != null)
                interactPromptText.text = message;
        }
    }

    /// <summary>
    /// 隐藏交互提示
    /// </summary>
    public void HideInteractPrompt()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    /// <summary>
    /// 关闭其他面板（保留指定面板）
    /// </summary>
    void CloseOtherPanels(GameObject except)
    {
        if (except != mainMenuPanel && mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (except != inventoryPanel && inventoryPanel != null) inventoryPanel.SetActive(false);
        if (except != orderPanel && orderPanel != null) orderPanel.SetActive(false);
        if (except != clueDiaryPanel && clueDiaryPanel != null) clueDiaryPanel.SetActive(false);
    }



    /// <summary>
    /// 显示技能栏（第三阶段解锁）
    /// </summary>
    public void UnlockSkillBar()
    {
        if (skillBarPanel != null)
            skillBarPanel.SetActive(true);
    }

    public void ShowGameUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform uiRoot = canvas.transform.Find("UIRoot");
            if (uiRoot != null)
            {
                foreach (Transform child in uiRoot)
                {
                    if (child.name == "MainMenuPanel" || child.name == "SettingsPanel" ||
                        child.name == "InventoryPanel" || child.name == "ClueDiaryPanel" ||
                        child.name == "OrderPanel" || child.name == "CraftingPanel" ||
                        child.name == "DialogPanel")
                    {
                        child.gameObject.SetActive(false);
                    }
                    else
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }

        if (skillBarPanel != null)
            skillBarPanel.SetActive(false);
    }

    public void HideGameUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            Transform uiRoot = canvas.transform.Find("UIRoot");
            if (uiRoot != null)
            {
                foreach (Transform child in uiRoot)
                {
                    if (child.name == "MainMenuPanel")
                    {
                        child.gameObject.SetActive(true);
                    }
                    else
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    void Update()
    {
        bool isUIOpen = (inventoryPanel != null && inventoryPanel.activeSelf) ||
                        (clueDiaryPanel != null && clueDiaryPanel.activeSelf) ||
                        (orderPanel != null && orderPanel.activeSelf);

        GameObject mainMenu = GameObject.Find("MainMenuPanel");
        bool isMainMenuOpen = mainMenu != null && mainMenu.activeSelf;

        GameObject settings = GameObject.Find("SettingsPanel");
        bool isSettingsOpen = settings != null && settings.activeSelf;

        Cursor.visible = isUIOpen || isMainMenuOpen || isSettingsOpen;
        Cursor.lockState = (isUIOpen || isMainMenuOpen || isSettingsOpen) ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
