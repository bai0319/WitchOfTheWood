using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIGenerator : MonoBehaviour
{
    public static UIGenerator instance;

    [Header("生成设置")]
    public bool generateOnStart = true;
    public TMP_FontAsset serifFont;

    [Header("颜色设置")]
    public Color parchmentColor = new Color(0.92f, 0.87f, 0.76f, 1f);
    public Color borderColor = new Color(0.3f, 0.2f, 0.1f, 1f);
    public Color textColor = Color.black;

    [Header("引用")]
    public Canvas mainCanvas;

    private GameObject uiRoot;
    private static TMP_FontAsset _defaultChineseFont;

    public static TMP_FontAsset GetDefaultChineseFont()
    {
        if (_defaultChineseFont != null && IsFontValid(_defaultChineseFont))
        {
            return _defaultChineseFont;
        }
        
        _defaultChineseFont = null; // 重置无效字体

        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        Debug.Log($"找到 {fonts.Length} 个TextMeshPro字体资源");
        
        foreach (var font in fonts)
        {
            if (IsFontValid(font) && font.name.Contains("msyh"))
            {
                _defaultChineseFont = font;
                Debug.Log($"找到中文字体(优先msyh): {font.name}");
                return font;
            }
        }

        foreach (var font in fonts)
        {
            if (IsFontValid(font))
            {
                if (font.name.Contains("Chinese") || font.name.Contains("中文") ||
                    font.name.Contains("宋体") || font.name.Contains("楷体") ||
                    font.name.Contains("黑体") || font.name.Contains("SimSun") ||
                    font.name.Contains("STKaiti") || font.name.Contains("SimHei") ||
                    font.name.Contains("Source Han") || font.name.Contains("Libian"))
                {
                    _defaultChineseFont = font;
                    Debug.Log($"找到中文字体: {font.name}");
                    return font;
                }
            }
        }

        foreach (var font in fonts)
        {
            if (IsFontValid(font))
            {
                _defaultChineseFont = font;
                Debug.Log($"使用任意可用字体: {font.name}");
                return font;
            }
        }

        Debug.LogWarning("未找到任何可用的TextMeshPro字体资源！请使用 ChineseFontImporter 导入中文字体。");
        return null;
    }

    private static bool IsFontValid(TMP_FontAsset font)
    {
        if (font == null) return false;
        if (font.atlasTextures == null || font.atlasTextures.Length == 0) return false;
        if (font.atlasTextures[0] == null) return false;
        return true;
    }

    void Awake()
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

    void Start()
    {
        textColor = new Color(textColor.r, textColor.g, textColor.b, 1f);
        if (generateOnStart)
        {
            GenerateAllUI();
        }
    }

    public void GenerateAllUI()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            DestroyImmediate(canvas.gameObject);
        }

        GameObject canvasObj = new GameObject("Canvas");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
#endif

        // 添加 EventSystem（处理按钮点击等交互事件）- 确保只有一个
        EventSystem[] existingEventSystems = FindObjectsOfType<EventSystem>();
        if (existingEventSystems.Length > 0)
        {
            for (int i = 1; i < existingEventSystems.Length; i++)
            {
                DestroyImmediate(existingEventSystems[i].gameObject);
            }
        }
        else
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create EventSystem");
#endif
        }

        // 确保 SceneTransitionManager 存在（跨场景持久化）
        if (SceneTransitionManager.instance == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            managerObj.AddComponent<SceneTransitionManager>();
        }

        // 确保 AutoCreateManagers 存在
        if (FindObjectOfType<AutoCreateManagers>() == null)
        {
            GameObject autoManagersObj = new GameObject("AutoCreateManagers");
            autoManagersObj.AddComponent<AutoCreateManagers>();
        }

        uiRoot = new GameObject("UIRoot");
        uiRoot.transform.SetParent(mainCanvas.transform);

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(uiRoot, "Create UIRoot");
#endif

        RectTransform uiRect = uiRoot.GetComponent<RectTransform>();
        if (uiRect == null) uiRect = uiRoot.AddComponent<RectTransform>();
        uiRect.anchorMin = Vector2.zero;
        uiRect.anchorMax = Vector2.one;
        uiRect.offsetMin = Vector2.zero;
        uiRect.offsetMax = Vector2.zero;

        GenerateDialogPanel();
        GenerateInventoryPanel();
        GenerateClueDiaryPanel();
        GenerateSkillBar();
        GenerateOrderPanel();
        GenerateCraftingPanel();
        GenerateInteractPrompt();
        GeneratePlayerName();

        UpdateUIManagerReferences();
        Debug.Log("UI生成完成，引用已更新");
    }

    GameObject CreateParchmentPanel(string name, float width, float height, Vector2 position, bool isCenter = true)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(uiRoot.transform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        if (isCenter)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
        rect.anchoredPosition = position;

        Image background = panel.AddComponent<Image>();
        background.color = parchmentColor;

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2, -2);

        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = borderColor;
        shadow.effectDistance = new Vector2(4, -4);

        return panel;
    }

    TextMeshProUGUI CreateText(string text, Transform parent, float fontSize = 16f, bool isBold = false, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmpro = textObj.AddComponent<TextMeshProUGUI>();
        tmpro.text = text;
        tmpro.fontSize = fontSize;
        tmpro.color = textColor;

        TMP_FontAsset fontToUse = serifFont != null ? serifFont : GetDefaultChineseFont();
        if (fontToUse != null)
        {
            tmpro.font = fontToUse;
        }

        tmpro.fontStyle = isBold ? FontStyles.Bold : FontStyles.Normal;
        tmpro.alignment = alignment;
        tmpro.verticalAlignment = VerticalAlignmentOptions.Middle;

        return tmpro;
    }

    Button CreateButton(string text, Transform parent, float width, float height, Vector2 position)
    {
        GameObject buttonObj = new GameObject("Button");
        buttonObj.transform.SetParent(parent);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.8f, 0.7f, 0.6f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.8f, 0.7f, 0.6f, 1f);
        colors.highlightedColor = new Color(0.9f, 0.85f, 0.75f, 1f);
        colors.pressedColor = new Color(0.7f, 0.6f, 0.5f, 1f);
        button.colors = colors;

        CreateText(text, buttonObj.transform, 16f);

        return button;
    }

    GameObject CreateScrollList(Transform parent, float width, float height, Vector2 position, out RectTransform content)
    {
        GameObject scrollObj = new GameObject("ScrollList");
        scrollObj.transform.SetParent(parent);

        RectTransform rect = scrollObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        Image bg = scrollObj.AddComponent<Image>();
        bg.color = new Color(0.95f, 0.9f, 0.85f, 1f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform);
        RectTransform viewRect = viewport.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(5, 5);
        viewRect.offsetMax = new Vector2(-5, -5);

        Image viewImage = viewport.AddComponent<Image>();
        viewImage.color = new Color(0.9f, 0.85f, 0.8f, 0.5f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform);
        content = contentObj.AddComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(0, 0);
        content.offsetMin = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewRect;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return scrollObj;
    }

    void GenerateDialogPanel()
    {
        GameObject panel = CreateParchmentPanel("DialogPanel", 700, 100, new Vector2(0, -300));
        panel.SetActive(false);

        GameObject speakerNameObj = new GameObject("SpeakerName");
        speakerNameObj.transform.SetParent(panel.transform);
        RectTransform nameRect = speakerNameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.02f, 0.7f);
        nameRect.anchorMax = new Vector2(0.3f, 1);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        TextMeshProUGUI speakerName = speakerNameObj.AddComponent<TextMeshProUGUI>();
        speakerName.text = "Abigail";
        speakerName.fontSize = 18f;
        speakerName.color = textColor;
        speakerName.fontStyle = FontStyles.Bold;
        speakerName.alignment = TextAlignmentOptions.Left;
        speakerName.verticalAlignment = VerticalAlignmentOptions.Middle;

        TMP_FontAsset fontToUse = serifFont != null ? serifFont : GetDefaultChineseFont();
        if (fontToUse != null)
        {
            speakerName.font = fontToUse;
        }

        GameObject contentObj = new GameObject("DialogContent");
        contentObj.transform.SetParent(panel.transform);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.02f, 0);
        contentRect.anchorMax = new Vector2(0.98f, 0.65f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        TextMeshProUGUI contentText = contentObj.AddComponent<TextMeshProUGUI>();
        contentText.text = "";
        contentText.fontSize = 18f;
        contentText.color = textColor;
        contentText.alignment = TextAlignmentOptions.Left;
        contentText.verticalAlignment = VerticalAlignmentOptions.Top;
        contentText.enableWordWrapping = true;

        if (fontToUse != null)
        {
            contentText.font = fontToUse;
        }

        if (DialogSystem.instance != null)
        {
            DialogSystem.instance.dialogPanel = panel;
            DialogSystem.instance.speakerNameText = speakerName;
            DialogSystem.instance.dialogContentText = contentText;
        }
    }

    void GenerateInventoryPanel()
    {
        GameObject panel = CreateParchmentPanel("InventoryPanel", 700, 800, new Vector2(0, 0));
        panel.SetActive(false);

        TMP_FontAsset fontToUse = FindWorkingFont();

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 80);
        titleRect.anchoredPosition = new Vector2(0, 320);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Inventory";
        titleText.fontSize = 48f;
        titleText.color = textColor;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) titleText.font = fontToUse;

        RectTransform contentRect;
        CreateScrollList(panel.transform, 640, 560, new Vector2(0, -100), out contentRect);

        Button closeButton = CreateButton("Close", panel.transform, 160, 60, new Vector2(0, -320));
        closeButton.onClick.AddListener(() => UIManager.instance?.ToggleInventory());

        GameObject totalTextObj = new GameObject("TotalItems");
        totalTextObj.transform.SetParent(panel.transform);
        RectTransform totalRect = totalTextObj.AddComponent<RectTransform>();
        totalRect.anchorMin = new Vector2(0.5f, 0.5f);
        totalRect.anchorMax = new Vector2(0.5f, 0.5f);
        totalRect.pivot = new Vector2(0.5f, 0.5f);
        totalRect.sizeDelta = new Vector2(600, 60);
        totalRect.anchoredPosition = new Vector2(0, -390);

        TextMeshProUGUI totalText = totalTextObj.AddComponent<TextMeshProUGUI>();
        totalText.text = "0 Items";
        totalText.fontSize = 32f;
        totalText.color = textColor;
        totalText.alignment = TextAlignmentOptions.Center;
        totalText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) totalText.font = fontToUse;

        if (InventorySystem.instance != null)
        {
            InventorySystem.instance.inventoryPanel = panel;
            InventorySystem.instance.itemListContent = contentRect;
            InventorySystem.instance.totalItemsText = totalText;
        }
    }

    void GenerateClueDiaryPanel()
    {
        GameObject panel = CreateParchmentPanel("ClueDiaryPanel", 1200, 1000, new Vector2(0, 0));
        panel.SetActive(false);

        TMP_FontAsset fontToUse = FindWorkingFont();

        // ===== 主标题 =====
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(800, 80);
        titleRect.anchoredPosition = new Vector2(0, 400);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Clue Diary";
        titleText.fontSize = 48f;
        titleText.color = textColor;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) titleText.font = fontToUse;

        // ===== 分类标签栏 =====
        float tabWidth = 260f;
        float tabHeight = 72f;
        float tabY = 380f;
        float tabSpacing = 30f;
        float totalTabWidth = tabWidth * 3 + tabSpacing * 2;
        float tabStartX = -totalTabWidth / 2 + tabWidth / 2;

        Button tabClue = CreateTabButton("Clues", panel.transform, tabWidth, tabHeight, new Vector2(tabStartX, tabY), fontToUse);
        Button tabPotion = CreateTabButton("Potions", panel.transform, tabWidth, tabHeight, new Vector2(tabStartX + tabWidth + tabSpacing, tabY), fontToUse);
        Button tabPlant = CreateTabButton("Plants", panel.transform, tabWidth, tabHeight, new Vector2(tabStartX + (tabWidth + tabSpacing) * 2, tabY), fontToUse);

        // ===== 页面标题 =====
        GameObject pageTitleObj = new GameObject("PageTitle");
        pageTitleObj.transform.SetParent(panel.transform);
        RectTransform pageTitleRect = pageTitleObj.AddComponent<RectTransform>();
        pageTitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageTitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageTitleRect.pivot = new Vector2(0.5f, 0.5f);
        pageTitleRect.sizeDelta = new Vector2(1000, 60);
        pageTitleRect.anchoredPosition = new Vector2(0, 240);

        TextMeshProUGUI pageTitleText = pageTitleObj.AddComponent<TextMeshProUGUI>();
        pageTitleText.text = "";
        pageTitleText.fontSize = 44f;
        pageTitleText.color = textColor;
        pageTitleText.fontStyle = FontStyles.Bold;
        pageTitleText.alignment = TextAlignmentOptions.Center;
        pageTitleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) pageTitleText.font = fontToUse;

        // ===== 分隔线 =====
        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(panel.transform);
        RectTransform dividerRect = dividerObj.AddComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.sizeDelta = new Vector2(1040, 6);
        dividerRect.anchoredPosition = new Vector2(0, 190);
        Image dividerImg = dividerObj.AddComponent<Image>();
        dividerImg.color = new Color(0.6f, 0.4f, 0.2f, 0.5f);

        // ===== 页面内容（可滚动区域） =====
        GameObject scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(panel.transform);
        RectTransform scrollRect_t = scrollObj.AddComponent<RectTransform>();
        scrollRect_t.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect_t.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect_t.pivot = new Vector2(0.5f, 0.5f);
        scrollRect_t.sizeDelta = new Vector2(1080, 520);
        scrollRect_t.anchoredPosition = new Vector2(0, -100);

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.93f, 0.88f, 0.82f, 0f);

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform);
        RectTransform viewRect = viewport.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = new Vector2(-50, -20);
        viewRect.offsetMax = new Vector2(50, 20);

        Image viewImage = viewport.AddComponent<Image>();
        viewImage.color = Color.clear;

        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewport.transform);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 0);
        contentRect.pivot = new Vector2(0.5f, 0);
        contentRect.sizeDelta = new Vector2(0, 500);

        TextMeshProUGUI pageContentText = contentObj.AddComponent<TextMeshProUGUI>();
        pageContentText.text = "";
        pageContentText.fontSize = 36f;
        pageContentText.color = textColor;
        pageContentText.alignment = TextAlignmentOptions.TopLeft;
        pageContentText.verticalAlignment = VerticalAlignmentOptions.Top;
        pageContentText.enableWordWrapping = true;
        pageContentText.rectTransform.anchorMin = Vector2.zero;
        pageContentText.rectTransform.anchorMax = Vector2.one;
        pageContentText.rectTransform.offsetMin = new Vector2(20, 20);
        pageContentText.rectTransform.offsetMax = new Vector2(-20, -20);
        if (fontToUse != null) pageContentText.font = fontToUse;

        CanvasGroup cg = contentObj.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        // ===== 底部分隔线 =====
        GameObject divider2Obj = new GameObject("Divider2");
        divider2Obj.transform.SetParent(panel.transform);
        RectTransform divider2Rect = divider2Obj.AddComponent<RectTransform>();
        divider2Rect.anchorMin = new Vector2(0.5f, 0.5f);
        divider2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        divider2Rect.pivot = new Vector2(0.5f, 0.5f);
        divider2Rect.sizeDelta = new Vector2(1040, 6);
        divider2Rect.anchoredPosition = new Vector2(0, -340);
        Image divider2Img = divider2Obj.AddComponent<Image>();
        divider2Img.color = new Color(0.6f, 0.4f, 0.2f, 0.5f);

        // ===== 分类标签文字（左下） =====
        GameObject categoryObj = new GameObject("CategoryText");
        categoryObj.transform.SetParent(panel.transform);
        RectTransform categoryRect = categoryObj.AddComponent<RectTransform>();
        categoryRect.anchorMin = new Vector2(0.5f, 0.5f);
        categoryRect.anchorMax = new Vector2(0.5f, 0.5f);
        categoryRect.pivot = new Vector2(0.5f, 0.5f);
        categoryRect.sizeDelta = new Vector2(400, 60);
        categoryRect.anchoredPosition = new Vector2(-400, -420);

        TextMeshProUGUI categoryText = categoryObj.AddComponent<TextMeshProUGUI>();
        categoryText.text = "Clues";
        categoryText.fontSize = 32f;
        categoryText.color = new Color(0.5f, 0.3f, 0.1f);
        categoryText.fontStyle = FontStyles.Bold;
        categoryText.alignment = TextAlignmentOptions.Left;
        categoryText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) categoryText.font = fontToUse;

        // ===== 页码显示（右下） =====
        GameObject pageNumObj = new GameObject("PageNumber");
        pageNumObj.transform.SetParent(panel.transform);
        RectTransform pageNumRect = pageNumObj.AddComponent<RectTransform>();
        pageNumRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageNumRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageNumRect.pivot = new Vector2(0.5f, 0.5f);
        pageNumRect.sizeDelta = new Vector2(400, 60);
        pageNumRect.anchoredPosition = new Vector2(400, -420);

        TextMeshProUGUI pageNumText = pageNumObj.AddComponent<TextMeshProUGUI>();
        pageNumText.text = "1 / 8";
        pageNumText.fontSize = 32f;
        pageNumText.color = new Color(0.5f, 0.3f, 0.1f);
        pageNumText.fontStyle = FontStyles.Bold;
        pageNumText.alignment = TextAlignmentOptions.Right;
        pageNumText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) pageNumText.font = fontToUse;

        // ===== 上一页/下一页按钮 =====
        Button prevBtn = CreateButton("← Prev", panel.transform, 260, 72, new Vector2(-220, -420));
        Button nextBtn = CreateButton("Next →", panel.transform, 260, 72, new Vector2(220, -420));

        // ===== 关闭按钮 =====
        Button closeBtn = CreateButton("Close", panel.transform, 160, 60, new Vector2(0, -420));
        closeBtn.onClick.AddListener(() => UIManager.instance?.ToggleClueDiary());

        // ===== 关联引用 =====
        SetupClueDiaryReferences(panel, pageTitleText, pageContentText, categoryText, 
            pageNumText, prevBtn, nextBtn, tabClue, tabPotion, tabPlant);
    }

    void SetupClueDiaryReferences(GameObject panel, TextMeshProUGUI pageTitleText, 
        TextMeshProUGUI pageContentText, TextMeshProUGUI categoryText, 
        TextMeshProUGUI pageNumText, Button prevBtn, Button nextBtn, 
        Button tabClue, Button tabPotion, Button tabPlant)
    {
        // 等待 ClueDiarySystem 实例化
        Invoke("DelayedSetupClueDiary", 0.1f);
    }

    void DelayedSetupClueDiary()
    {
        if (ClueDiarySystem.instance == null)
        {
            Debug.LogWarning("ClueDiarySystem.instance 为空，等待再次尝试...");
            Invoke("DelayedSetupClueDiary", 0.2f);
            return;
        }

        GameObject panel = uiRoot.transform.Find("ClueDiaryPanel")?.gameObject;
        if (panel == null)
        {
            Debug.LogError("ClueDiaryPanel 未找到！");
            return;
        }

        ClueDiarySystem.instance.clueDiaryPanel = panel;

        Transform pageTitle = panel.transform.Find("PageTitle");
        if (pageTitle != null)
            ClueDiarySystem.instance.pageTitleText = pageTitle.GetComponent<TextMeshProUGUI>();

        Transform pageContent = panel.transform.Find("ScrollArea/Viewport/Content");
        if (pageContent != null)
            ClueDiarySystem.instance.pageContentText = pageContent.GetComponent<TextMeshProUGUI>();

        Transform categoryText = panel.transform.Find("CategoryText");
        if (categoryText != null)
            ClueDiarySystem.instance.pageCategoryText = categoryText.GetComponent<TextMeshProUGUI>();

        Transform pageNumText = panel.transform.Find("PageNumber");
        if (pageNumText != null)
            ClueDiarySystem.instance.pageNumberText = pageNumText.GetComponent<TextMeshProUGUI>();

        // 查找按钮 - 遍历所有子对象
        Button[] allButtons = panel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons)
        {
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                string txt = btnText.text;
                if (txt.Contains("Prev"))
                    ClueDiarySystem.instance.prevPageButton = btn;
                else if (txt.Contains("Next"))
                    ClueDiarySystem.instance.nextPageButton = btn;
                else if (txt == "Clues")
                    ClueDiarySystem.instance.tabClueButton = btn;
                else if (txt == "Potions")
                    ClueDiarySystem.instance.tabPotionButton = btn;
                else if (txt == "Plants")
                    ClueDiarySystem.instance.tabPlantButton = btn;
            }
        }

        // 绑定按钮事件
        if (ClueDiarySystem.instance.prevPageButton != null)
            ClueDiarySystem.instance.prevPageButton.onClick.AddListener(() => ClueDiarySystem.instance.GoToPrevPage());
        if (ClueDiarySystem.instance.nextPageButton != null)
            ClueDiarySystem.instance.nextPageButton.onClick.AddListener(() => ClueDiarySystem.instance.GoToNextPage());
        if (ClueDiarySystem.instance.tabClueButton != null)
            ClueDiarySystem.instance.tabClueButton.onClick.AddListener(() => ClueDiarySystem.instance.ShowCluePages());
        if (ClueDiarySystem.instance.tabPotionButton != null)
            ClueDiarySystem.instance.tabPotionButton.onClick.AddListener(() => ClueDiarySystem.instance.ShowPotionPages());
        if (ClueDiarySystem.instance.tabPlantButton != null)
            ClueDiarySystem.instance.tabPlantButton.onClick.AddListener(() => ClueDiarySystem.instance.ShowPlantPages());

        ClueDiarySystem.instance.UpdatePageDisplay();
    }

    Button CreateTabButton(string text, Transform parent, float width, float height, Vector2 position, TMP_FontAsset font)
    {
        GameObject buttonObj = new GameObject("Tab_" + text);
        buttonObj.transform.SetParent(parent);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.85f, 0.75f, 0.6f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.85f, 0.75f, 0.6f, 1f);
        colors.highlightedColor = new Color(0.95f, 0.85f, 0.7f, 1f);
        colors.pressedColor = new Color(0.7f, 0.6f, 0.5f, 1f);
        button.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = 18f;
        btnText.color = textColor;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (font != null) btnText.font = font;

        return button;
    }

    void GenerateSkillBar()
    {
        GameObject panel = new GameObject("SkillBarPanel");
        panel.transform.SetParent(uiRoot.transform);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 60);
        rect.anchorMin = new Vector2(0.5f, 0.02f);
        rect.anchorMax = new Vector2(0.5f, 0.02f);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 30);

        Image background = panel.AddComponent<Image>();
        background.color = parchmentColor;

        float buttonWidth = 50;
        float spacing = 20;
        float startX = -(buttonWidth + spacing);

        CreateSkillButton("Q", "Whistle", panel.transform, buttonWidth, 50, new Vector2(startX, 0));
        CreateSkillButton("F", "Scare Wolves", panel.transform, buttonWidth, 50, new Vector2(startX + buttonWidth + spacing, 0));

        if (SkillBarSystem.instance != null)
        {
            SkillBarSystem.instance.skillBarPanel = panel;
        }
    }

    void CreateSkillButton(string key, string name, Transform parent, float width, float height, Vector2 position)
    {
        GameObject buttonObj = new GameObject("Skill_" + key);
        buttonObj.transform.SetParent(parent);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.7f, 0.65f, 0.55f, 1f);

        Button button = buttonObj.AddComponent<Button>();

        GameObject keyObj = new GameObject("Key");
        keyObj.transform.SetParent(buttonObj.transform);
        RectTransform keyRect = keyObj.AddComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0, 0.5f);
        keyRect.anchorMax = new Vector2(1, 1);

        TextMeshProUGUI keyText = keyObj.AddComponent<TextMeshProUGUI>();
        keyText.text = key;
        keyText.fontSize = 18f;
        keyText.color = textColor;
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Center;
        keyText.verticalAlignment = VerticalAlignmentOptions.Middle;

        TMP_FontAsset fontToUse = serifFont != null ? serifFont : GetDefaultChineseFont();
        if (fontToUse != null)
        {
            keyText.font = fontToUse;
        }

        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(buttonObj.transform);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = new Vector2(1, 0.5f);

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = name;
        nameText.fontSize = 12f;
        nameText.color = textColor;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.verticalAlignment = VerticalAlignmentOptions.Middle;

        if (fontToUse != null)
        {
            nameText.font = fontToUse;
        }
    }

    void GenerateOrderPanel()
    {
        GameObject panel = CreateParchmentPanel("OrderPanel", 900, 800, new Vector2(0, 0));
        panel.SetActive(false);

        TMP_FontAsset fontToUse = FindWorkingFont();

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(800, 80);
        titleRect.anchoredPosition = new Vector2(0, 320);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Orders";
        titleText.fontSize = 48f;
        titleText.color = textColor;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) titleText.font = fontToUse;

        RectTransform contentRect;
        CreateScrollList(panel.transform, 840, 560, new Vector2(0, -100), out contentRect);

        Button closeButton = CreateButton("Close", panel.transform, 160, 60, new Vector2(0, -360));
        closeButton.onClick.AddListener(() => UIManager.instance?.ToggleOrderPanel());

        if (OrderPanelSystem.instance != null)
        {
            OrderPanelSystem.instance.orderPanel = panel;
            OrderPanelSystem.instance.orderListContent = contentRect;
        }
    }

    void GenerateCraftingPanel()
    {
        GameObject panel = CreateParchmentPanel("CraftingPanel", 650, 350, new Vector2(0, 0));
        panel.SetActive(false);

        TMP_FontAsset fontToUse = FindWorkingFont();

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(600, 40);
        titleRect.anchoredPosition = new Vector2(0, 135);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Potion Crafting";
        titleText.fontSize = 24f;
        titleText.color = textColor;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) titleText.font = fontToUse;

        GameObject gridObj = new GameObject("CraftingGrid");
        gridObj.transform.SetParent(panel.transform);
        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.sizeDelta = new Vector2(270, 270);
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(-130, -30);

        Image gridBg = gridObj.AddComponent<Image>();
        gridBg.color = new Color(0.85f, 0.8f, 0.75f, 1f);

        float cellSize = 30;
        float spacing = 0;

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                GameObject cell = new GameObject($"Cell_{row}_{col}");
                cell.transform.SetParent(gridObj.transform);

                RectTransform cellRect = cell.AddComponent<RectTransform>();
                cellRect.sizeDelta = new Vector2(cellSize, cellSize);
                cellRect.anchorMin = new Vector2(0, 1);
                cellRect.anchorMax = new Vector2(0, 1);
                cellRect.pivot = new Vector2(0, 1);
                cellRect.anchoredPosition = new Vector2(col * (cellSize + spacing), -row * (cellSize + spacing));

                Image cellImage = cell.AddComponent<Image>();
                cellImage.color = new Color(0.9f, 0.88f, 0.85f, 1f);

                Outline outline = cell.AddComponent<Outline>();
                outline.effectColor = borderColor;
                outline.effectDistance = new Vector2(1, -1);
            }
        }

        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(panel.transform);
        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(60, 60);
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(20, -30);

        TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "→";
        arrowText.fontSize = 36f;
        arrowText.color = borderColor;
        arrowText.alignment = TextAlignmentOptions.Center;

        GameObject resultObj = new GameObject("ResultSlot");
        resultObj.transform.SetParent(panel.transform);
        RectTransform resultRect = resultObj.AddComponent<RectTransform>();
        resultRect.sizeDelta = new Vector2(80, 80);
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = new Vector2(150, -30);

        Image resultImage = resultObj.AddComponent<Image>();
        resultImage.color = new Color(0.95f, 0.9f, 0.85f, 1f);

        Outline resultOutline = resultObj.AddComponent<Outline>();
        resultOutline.effectColor = borderColor;
        resultOutline.effectDistance = new Vector2(2, -2);

        Button closeButton = CreateButton("关闭", panel.transform, 80, 30, new Vector2(0, -160));
        closeButton.onClick.AddListener(() => panel.SetActive(false));
    }

    void GenerateInteractPrompt()
    {
        GameObject prompt = new GameObject("InteractPrompt");
        prompt.transform.SetParent(uiRoot.transform);
        prompt.SetActive(false);

        RectTransform rect = prompt.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 40);
        rect.anchorMin = new Vector2(0.5f, 0.3f);
        rect.anchorMax = new Vector2(0.5f, 0.3f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image bg = prompt.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.2f, 0.1f, 0.8f);

        TextMeshProUGUI text = prompt.AddComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = "[E] Interact";
            text.fontSize = 18f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;

            TMP_FontAsset fontToUse = serifFont != null ? serifFont : GetDefaultChineseFont();
            if (fontToUse != null)
            {
                text.font = fontToUse;
            }
        }

        if (UIManager.instance != null)
        {
            UIManager.instance.interactPrompt = prompt;
            if (text != null)
            {
                UIManager.instance.interactPromptText = text;
            }
        }
    }

    void GeneratePlayerName()
    {
        GameObject nameObj = new GameObject("PlayerName");
        nameObj.transform.SetParent(uiRoot.transform);

        RectTransform rect = nameObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 25);
        rect.anchorMin = new Vector2(0.5f, 0.85f);
        rect.anchorMax = new Vector2(0.5f, 0.85f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI text = nameObj.AddComponent<TextMeshProUGUI>();
        text.text = "Abigail";
        text.fontSize = 16f;
        text.color = textColor;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.Center;

        TMP_FontAsset fontToUse = serifFont != null ? serifFont : GetDefaultChineseFont();
        if (fontToUse != null)
        {
            text.font = fontToUse;
        }

        if (UIManager.instance != null)
        {
            UIManager.instance.playerNameText = text;
        }
    }

    void UpdateUIManagerReferences()
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.inventoryPanel = uiRoot.transform.Find("InventoryPanel")?.gameObject;
            UIManager.instance.clueDiaryPanel = uiRoot.transform.Find("ClueDiaryPanel")?.gameObject;
            UIManager.instance.orderPanel = uiRoot.transform.Find("OrderPanel")?.gameObject;
            UIManager.instance.skillBarPanel = uiRoot.transform.Find("SkillBarPanel")?.gameObject;
            UIManager.instance.dialogPanel = uiRoot.transform.Find("DialogPanel")?.gameObject;
            UIManager.instance.interactPrompt = uiRoot.transform.Find("InteractPrompt")?.gameObject;

            Transform playerNameTrans = uiRoot.transform.Find("PlayerName");
            if (playerNameTrans != null)
            {
                UIManager.instance.playerNameText = playerNameTrans.GetComponent<TextMeshProUGUI>();
            }

            Transform promptTextTrans = uiRoot.transform.Find("InteractPrompt");
            if (promptTextTrans != null)
            {
                UIManager.instance.interactPromptText = promptTextTrans.GetComponent<TextMeshProUGUI>();
            }
        }

        if (InventorySystem.instance != null)
        {
            InventorySystem.instance.inventoryPanel = uiRoot.transform.Find("InventoryPanel")?.gameObject;
            InventorySystem.instance.itemListContent = uiRoot.transform.Find("InventoryPanel/ScrollList/Viewport/Content");

            Transform totalText = uiRoot.transform.Find("InventoryPanel/TotalItems");
            if (totalText != null)
            {
                InventorySystem.instance.totalItemsText = totalText.GetComponent<TextMeshProUGUI>();
            }
        }

        // ClueDiarySystem 的引用在 DelayedSetupClueDiary 中设置

        if (SkillBarSystem.instance != null)
        {
            SkillBarSystem.instance.skillBarPanel = uiRoot.transform.Find("SkillBarPanel")?.gameObject;
        }

        if (OrderPanelSystem.instance != null)
        {
            OrderPanelSystem.instance.orderPanel = uiRoot.transform.Find("OrderPanel")?.gameObject;
            OrderPanelSystem.instance.orderListContent = uiRoot.transform.Find("OrderPanel/ScrollList/Viewport/Content");
        }

        SetupMainMenuAndSettings();
    }

    void SetupMainMenuAndSettings()
    {
        GameObject uiManagerObj = GameObject.Find("UIManager");
        if (uiManagerObj == null) return;

        MainMenu mainMenu = uiManagerObj.GetComponent<MainMenu>();
        if (mainMenu == null)
        {
            mainMenu = uiManagerObj.AddComponent<MainMenu>();
        }

        GameSettings gameSettings = uiManagerObj.GetComponent<GameSettings>();
        if (gameSettings == null)
        {
            gameSettings = uiManagerObj.AddComponent<GameSettings>();
        }

        GenerateMainMenu(mainMenu);
        GenerateSettingsPanel(gameSettings);
    }

    void GenerateMainMenu(MainMenu mainMenu)
    {
        GameObject panel = new GameObject("MainMenuPanel");
        panel.transform.SetParent(uiRoot.transform);
        panel.transform.SetAsFirstSibling();

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.1f, 0.15f, 0.1f, 1f);
        mainMenu.backgroundImage = background;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1400, 120);
        titleRect.anchoredPosition = new Vector2(0, 300);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Witch of the Wood";
        titleText.fontSize = 120f;
        titleText.color = new Color(0.9f, 0.85f, 0.75f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;

        TMP_FontAsset fontToUse = FindWorkingFont();
        if (fontToUse != null) titleText.font = fontToUse;

        GameObject subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(panel.transform);
        RectTransform subtitleRect = subtitleObj.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRect.pivot = new Vector2(0.5f, 0.5f);
        subtitleRect.sizeDelta = new Vector2(1200, 80);
        subtitleRect.anchoredPosition = new Vector2(0, 150);

        TextMeshProUGUI subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "A Tale of Magic and Mystery";
        subtitleText.fontSize = 40f;
        subtitleText.color = new Color(0.7f, 0.65f, 0.55f);
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) subtitleText.font = fontToUse;

        Button startBtn = CreateDarkGreenButton("Start Game", panel.transform, 500, 120, new Vector2(0, 0));
        startBtn.onClick.AddListener(mainMenu.StartGame);
        mainMenu.startButton = startBtn;

        Button exitBtn = CreateDarkGreenButton("Exit Game", panel.transform, 500, 120, new Vector2(0, -180));
        exitBtn.onClick.AddListener(mainMenu.ExitGame);
        mainMenu.exitButton = exitBtn;

        mainMenu.mainMenuPanel = panel;
        panel.SetActive(true);

        foreach (Transform child in uiRoot.transform)
        {
            if (child.gameObject != panel)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    void GenerateSettingsPanel(GameSettings gameSettings)
    {
        GameObject panel = CreateParchmentPanel("SettingsPanel", 1200, 1000, new Vector2(0, 0));
        panel.SetActive(false);

        Image bgImage = panel.GetComponent<Image>();
        if (bgImage != null)
        {
            bgImage.color = new Color(0.15f, 0.2f, 0.15f, 0.95f);
        }

        TMP_FontAsset fontToUse = FindWorkingFont();

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1000, 100);
        titleRect.anchoredPosition = new Vector2(0, 400);

        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Settings";
        titleText.fontSize = 64f;
        titleText.color = new Color(0.9f, 0.85f, 0.75f);
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
        if (fontToUse != null) titleText.font = fontToUse;

        GameObject controlsObj = new GameObject("ControlsText");
        controlsObj.transform.SetParent(panel.transform);
        RectTransform controlsRect = controlsObj.AddComponent<RectTransform>();
        controlsRect.anchorMin = new Vector2(0.5f, 0.5f);
        controlsRect.anchorMax = new Vector2(0.5f, 0.5f);
        controlsRect.pivot = new Vector2(0.5f, 0.5f);
        controlsRect.sizeDelta = new Vector2(1080, 560);
        controlsRect.anchoredPosition = new Vector2(0, 0);

        TextMeshProUGUI controlsText = controlsObj.AddComponent<TextMeshProUGUI>();
        controlsText.text = "Controls\n\n" +
            "WASD / Arrow Keys - Move\n" +
            "Mouse - Look Around\n" +
            "E - Interact\n" +
            "Q - Whistle\n" +
            "F - Scare Wolves\n" +
            "I - Inventory\n" +
            "J - Clue Diary\n" +
            "O - Orders\n" +
            "C - Crafting\n" +
            "ESC - Pause / Settings";
        controlsText.fontSize = 32f;
        controlsText.color = new Color(0.8f, 0.75f, 0.65f);
        controlsText.alignment = TextAlignmentOptions.Left;
        controlsText.verticalAlignment = VerticalAlignmentOptions.Top;
        controlsText.enableWordWrapping = true;
        if (fontToUse != null) controlsText.font = fontToUse;
        gameSettings.controlsText = controlsText;

        Button continueBtn = CreateDarkGreenButton("Continue", panel.transform, 280, 80, new Vector2(-300, -400));
        continueBtn.onClick.AddListener(gameSettings.ContinueGame);
        gameSettings.continueButton = continueBtn;

        Button exitMenuBtn = CreateDarkGreenButton("Exit to Menu", panel.transform, 280, 80, new Vector2(0, -400));
        exitMenuBtn.onClick.AddListener(gameSettings.ExitToMainMenu);
        gameSettings.exitToMenuButton = exitMenuBtn;

        Button exitGameBtn = CreateDarkGreenButton("Exit Game", panel.transform, 280, 80, new Vector2(300, -400));
        exitGameBtn.onClick.AddListener(gameSettings.ExitGame);
        gameSettings.exitGameButton = exitGameBtn;

        gameSettings.settingsPanel = panel;
        panel.SetActive(false);
    }

    Button CreateDarkGreenButton(string text, Transform parent, float width, float height, Vector2 position)
    {
        GameObject buttonObj = new GameObject("Button_" + text);
        buttonObj.transform.SetParent(parent);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.15f, 0.4f, 0.2f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.4f, 0.2f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.6f, 0.35f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.15f, 1f);
        colors.disabledColor = new Color(0.3f, 0.4f, 0.3f, 0.5f);
        button.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI btnText = textObj.AddComponent<TextMeshProUGUI>();
        btnText.text = text;
        btnText.fontSize = 48f;
        btnText.color = new Color(0.95f, 0.98f, 0.9f);
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.verticalAlignment = VerticalAlignmentOptions.Middle;
        btnText.enableWordWrapping = false;

        TMP_FontAsset fontToUse = FindWorkingFont();
        if (fontToUse != null)
        {
            btnText.font = fontToUse;
            Debug.Log($"按钮文字使用字体: {fontToUse.name}");
        }
        else
        {
            Debug.LogWarning("按钮文字未找到可用字体，使用默认字体");
        }

        return button;
    }

    TMP_FontAsset FindWorkingFont()
    {
        if (serifFont != null) return serifFont;

        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in fonts)
        {
            if (font != null && font.atlasTextures != null && font.atlasTextures.Length > 0 && font.atlasTextures[0] != null)
            {
                return font;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    [ContextMenu("在编辑器中生成UI（永久保存）")]
    void GenerateUIInEditor()
    {
        GenerateAllUI();
        EditorUtility.SetDirty(gameObject);
        AssetDatabase.SaveAssets();
        Debug.Log("UI已在编辑器中生成并保存！");
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIGenerator))]
public class UIGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UIGenerator generator = (UIGenerator)target;

        GUILayout.Space(10);
        GUILayout.Label("中文字体设置", EditorStyles.boldLabel);

        if (generator.serifFont == null)
        {
            GUILayout.Label("未设置中文字体，中文将显示为方块", EditorStyles.helpBox);

            if (GUILayout.Button("自动导入中文字体"))
            {
                if (ChineseFontImporter.ImportChineseFont())
                {
                    TMP_FontAsset fontAsset = ChineseFontImporter.GetOrCreateChineseFontAsset();
                    if (fontAsset != null)
                    {
                        generator.serifFont = fontAsset;
                        EditorUtility.SetDirty(generator);
                        AssetDatabase.SaveAssets();
                        Debug.Log("中文字体已设置完成！");
                    }
                }
            }
        }
        else
        {
            GUILayout.Label($"当前字体: {generator.serifFont.name}", EditorStyles.helpBox);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("在编辑器中生成UI（永久保存）"))
        {
            generator.GenerateAllUI();
            EditorUtility.SetDirty(generator.gameObject);
            if (generator.mainCanvas != null)
            {
                EditorUtility.SetDirty(generator.mainCanvas.gameObject);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("UI已在编辑器中生成并保存！");
        }
    }
}
#endif