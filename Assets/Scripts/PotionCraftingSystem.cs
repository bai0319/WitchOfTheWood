using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 药剂制作系统 - 9×9方格合成界面
/// </summary>
public class PotionCraftingSystem : MonoBehaviour
{
    public static PotionCraftingSystem instance;

    [Header("UI引用")]
    public GameObject craftingPanel;
    public Transform gridContainer;           // 9×9方格容器
    public Transform resultSlot;              // 结果输出槽
    public Button craftButton;                // 制作按钮

    [Header("药剂配方")]
    public List<PotionRecipe> recipes = new List<PotionRecipe>();

    private CraftingSlot[,] craftingGrid = new CraftingSlot[9, 9];
    private ItemSlotUI resultSlotUI;
    private bool gridInitialized = false;

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

    void Start()
    {
        InitializeGrid();
        InitializeRecipes();

        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        if (craftButton != null)
            craftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    /// <summary>
    /// 初始化9×9方格
    /// </summary>
    void InitializeGrid()
    {
        if (gridContainer == null) return;

        // 创建9×9的方格
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                GameObject cell = CreateCraftingCell();
                cell.transform.SetParent(gridContainer);
                cell.transform.localScale = Vector3.one;

                CraftingSlot slot = cell.GetComponent<CraftingSlot>();
                if (slot != null)
                {
                    slot.Initialize(x, y, this);
                    craftingGrid[x, y] = slot;
                }
            }
        }
    }

    /// <summary>
    /// 自动创建方格单元
    /// </summary>
    GameObject CreateCraftingCell()
    {
        GameObject cell = new GameObject("CraftingCell");

        RectTransform rect = cell.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);

        Image image = cell.AddComponent<Image>();
        image.color = new Color(0.85f, 0.8f, 0.7f, 1f);

        // 边框
        Outline outline = cell.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.2f, 0.1f, 1f);
        outline.effectDistance = new Vector2(1, -1);

        // 物品图标
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(cell.transform);

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(2, 2);
        iconRect.offsetMax = new Vector2(-2, -2);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.enabled = false;

        // 物品名称文字
        GameObject textObj = new GameObject("ItemName");
        textObj.transform.SetParent(cell.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 10f;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.verticalAlignment = VerticalAlignmentOptions.Middle;

        // 添加按钮组件
        Button button = cell.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        // 添加CraftingSlot组件
        CraftingSlot slot = cell.AddComponent<CraftingSlot>();
        slot.itemIcon = iconImage;
        slot.itemNameText = text;

        return cell;
    }

    /// <summary>
    /// 初始化药剂配方
    /// </summary>
    void InitializeRecipes()
    {
        recipes.Clear();

        // 止痛药：金盏花×2 + 鼠尾草×1 → 5点
        recipes.Add(new PotionRecipe("止痛药", new Dictionary<string, int>
        {
            { "金盏花", 2 },
            { "鼠尾草", 1 }
        }, 5, "高"));

        // 催眠药：洋甘菊×2 + 薰衣草×1 → 8点
        recipes.Add(new PotionRecipe("催眠药", new Dictionary<string, int>
        {
            { "洋甘菊", 2 },
            { "薰衣草", 1 }
        }, 8, "中"));

        // 治愈药：金盏花×1 + 柳树皮×2 + 康复草×1 → 12点
        recipes.Add(new PotionRecipe("治愈药", new Dictionary<string, int>
        {
            { "金盏花", 1 },
            { "柳树皮", 2 },
            { "康复草", 1 }
        }, 12, "低"));

        // 驱寒药：鼠尾草×2 + 百里香×2 → 6点
        recipes.Add(new PotionRecipe("驱寒药", new Dictionary<string, int>
        {
            { "鼠尾草", 2 },
            { "百里香", 2 }
        }, 6, "高"));
    }

    /// <summary>
    /// 打开药剂制作界面
    /// </summary>
    public void OpenCraftingPanel()
    {
        if (craftingPanel != null)
            craftingPanel.SetActive(true);

        // 第一次打开时初始化网格
        if (!gridInitialized)
        {
            InitializeGrid();
            gridInitialized = true;
        }

        UpdateResultPreview();
    }

    /// <summary>
    /// 关闭药剂制作界面
    /// </summary>
    public void CloseCraftingPanel()
    {
        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        ClearGrid();
    }

    /// <summary>
    /// 清空方格
    /// </summary>
    void ClearGrid()
    {
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                craftingGrid[x, y]?.Clear();
            }
        }
        UpdateResultPreview();
    }

    /// <summary>
    /// 添加物品到方格
    /// </summary>
    public void AddItemToGrid(string itemName)
    {
        // 找一个空的格子
        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (craftingGrid[x, y] != null && craftingGrid[x, y].IsEmpty())
                {
                    craftingGrid[x, y].SetItem(itemName);
                    UpdateResultPreview();
                    return;
                }
            }
        }

        DialogSystem.instance?.StartDialog("方格已满！");
    }

    /// <summary>
    /// 从方格移除物品
    /// </summary>
    public void RemoveItemFromGrid(int x, int y)
    {
        if (craftingGrid[x, y] != null && !craftingGrid[x, y].IsEmpty())
        {
            string itemName = craftingGrid[x, y].GetItemName();
            craftingGrid[x, y].Clear();

            // 归还到背包
            InventorySystem.instance?.AddItem(itemName, 1);
            UpdateResultPreview();
        }
    }

    /// <summary>
    /// 更新结果预览
    /// </summary>
    void UpdateResultPreview()
    {
        PotionRecipe matchedRecipe = CheckRecipe();

        if (matchedRecipe != null)
        {
            if (resultSlotUI != null)
            {
                resultSlotUI.Setup(new InventorySystem.InventoryItem(
                    matchedRecipe.potionName, 1, $"效果：{matchedRecipe.value}点"));
            }
        }
        else
        {
            // 清空结果槽
            if (resultSlotUI != null)
            {
                resultSlotUI.Setup(null);
            }
        }
    }

    /// <summary>
    /// 检查是否匹配配方
    /// </summary>
    PotionRecipe CheckRecipe()
    {
        // 收集当前方格中的所有物品
        Dictionary<string, int> currentItems = new Dictionary<string, int>();

        for (int y = 0; y < 9; y++)
        {
            for (int x = 0; x < 9; x++)
            {
                if (craftingGrid[x, y] != null && !craftingGrid[x, y].IsEmpty())
                {
                    string itemName = craftingGrid[x, y].GetItemName();
                    if (currentItems.ContainsKey(itemName))
                    {
                        currentItems[itemName]++;
                    }
                    else
                    {
                        currentItems[itemName] = 1;
                    }
                }
            }
        }

        // 检查是否匹配任何配方
        foreach (PotionRecipe recipe in recipes)
        {
            if (recipe.Matches(currentItems))
            {
                return recipe;
            }
        }

        return null;
    }

    /// <summary>
    /// 制作按钮点击
    /// </summary>
    void OnCraftButtonClicked()
    {
        PotionRecipe matchedRecipe = CheckRecipe();

        if (matchedRecipe != null)
        {
            // 扣除材料
            foreach (var item in matchedRecipe.requiredHerbs)
            {
                InventorySystem.instance?.RemoveItem(item.Key, item.Value);
            }

            // 清空方格
            ClearGrid();

            // 添加药剂到背包
            InventorySystem.instance?.AddItem(matchedRecipe.potionName, 1);

            // 显示成功提示
            DialogSystem.instance?.StartDialog($"制作成功！获得「{matchedRecipe.potionName}」，价值{matchedRecipe.value}点。");
        }
        else
        {
            DialogSystem.instance?.StartDialog("配方不正确，无法制作。");
        }
    }

    /// <summary>
    /// 药剂配方数据
    /// </summary>
    [System.Serializable]
    public class PotionRecipe
    {
        public string potionName;                 // 药剂名称
        public Dictionary<string, int> requiredHerbs; // 所需药草
        public int value;                         // 获得数值
        public string frequency;                  // 接单频率

        public PotionRecipe(string name, Dictionary<string, int> herbs, int val, string freq)
        {
            potionName = name;
            requiredHerbs = herbs;
            value = val;
            frequency = freq;
        }

        /// <summary>
        /// 检查是否匹配配方
        /// </summary>
        public bool Matches(Dictionary<string, int> items)
        {
            if (items.Count != requiredHerbs.Count)
                return false;

            foreach (var herb in requiredHerbs)
            {
                if (!items.ContainsKey(herb.Key) || items[herb.Key] != herb.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

/// <summary>
/// 制作槽位组件
/// </summary>
public class CraftingSlot : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;

    private int gridX;
    private int gridY;
    private PotionCraftingSystem craftingSystem;
    private string currentItemName;

    /// <summary>
    /// 初始化槽位
    /// </summary>
    public void Initialize(int x, int y, PotionCraftingSystem system)
    {
        gridX = x;
        gridY = y;
        craftingSystem = system;
        currentItemName = null;

        // 添加点击事件
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    /// <summary>
    /// 设置物品
    /// </summary>
    public void SetItem(string itemName)
    {
        currentItemName = itemName;

        if (!string.IsNullOrEmpty(itemName))
        {
            if (itemNameText != null)
                itemNameText.text = itemName;
            if (itemIcon != null)
                itemIcon.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void Clear()
    {
        currentItemName = null;
        if (itemNameText != null)
            itemNameText.text = "";
        if (itemIcon != null)
            itemIcon.enabled = false;
    }

    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(currentItemName);
    }

    /// <summary>
    /// 获取物品名称
    /// </summary>
    public string GetItemName()
    {
        return currentItemName;
    }

    /// <summary>
    /// 点击槽位
    /// </summary>
    void OnClick()
    {
        if (IsEmpty())
        {
            // 尝试从背包添加物品
            if (InventorySystem.instance != null)
            {
                // 获取背包中的药草
                var herbs = InventorySystem.instance.GetHerbs();
                if (herbs.Count > 0)
                {
                    // 默认添加第一个药草
                    string herbName = herbs[0].itemName;
                    if (InventorySystem.instance.HasEnoughItems(herbName, 1))
                    {
                        InventorySystem.instance.RemoveItem(herbName, 1);
                        SetItem(herbName);
                    }
                }
            }
        }
        else
        {
            // 移除物品
            craftingSystem?.RemoveItemFromGrid(gridX, gridY);
        }
    }
}
