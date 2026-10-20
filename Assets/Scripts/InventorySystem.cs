using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 背包系统 - 管理药草/魔药存量
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem instance;

    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;        // 物品名称
        public int quantity;           // 数量
        public Sprite itemIcon;        // 物品图标（可选）
        [TextArea(1, 3)]
        public string description;     // 物品描述

        public InventoryItem(string name, int qty, string desc = "")
        {
            itemName = name;
            quantity = qty;
            description = desc;
        }
    }

    public List<InventoryItem> inventoryItems = new List<InventoryItem>();

    [Header("UI引用")]
    public GameObject inventoryPanel;
    public Transform itemListContent;          // 物品列表容器
    public TextMeshProUGUI totalItemsText;     // 总物品数量文本

    [Header("Item Types")]
    public string[] herbNames = { "Moonlight Grass", "Starlight Flower", "Blackberry", "Poison Mushroom", "Mandrake" };
    public string[] potionNames = { "Healing Potion", "Mana Potion", "Invisibility Potion", "Transformation Potion" };

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
        // 初始化背包
        InitializeDefaultItems();
        UpdateInventoryUI();
    }

    /// <summary>
    /// 初始化默认物品
    /// </summary>
    void InitializeDefaultItems()
    {
        // 添加默认药草
        foreach (string herb in herbNames)
        {
            inventoryItems.Add(new InventoryItem(herb, Random.Range(0, 5), GetHerbDescription(herb)));
        }

        // 添加默认魔药
        foreach (string potion in potionNames)
        {
            inventoryItems.Add(new InventoryItem(potion, Random.Range(0, 3), GetPotionDescription(potion)));
        }
    }

    string GetHerbDescription(string herbName)
    {
        switch (herbName)
        {
            case "Moonlight Grass": return "A silver herb that glimmers in moonlight, used for healing potions";
            case "Starlight Flower": return "A flower that only blooms at night, main ingredient for mana potions";
            case "Blackberry": return "A common wild berry, usable for basic potions";
            case "Poison Mushroom": return "A mildly toxic fungus, handle with care";
            case "Mandrake": return "A legendary magical herb with a human-shaped root";
            default: return "";
        }
    }

    string GetPotionDescription(string potionName)
    {
        switch (potionName)
        {
            case "Healing Potion": return "A red potion that restores health";
            case "Mana Potion": return "A blue potion that restores mana";
            case "Invisibility Potion": return "A transparent potion that grants brief invisibility";
            case "Transformation Potion": return "A strange potion that transforms you into another creature";
            default: return "";
        }
    }

    /// <summary>
    /// 添加物品
    /// </summary>
    public void AddItem(string itemName, int quantity = 1)
    {
        InventoryItem existingItem = inventoryItems.Find(item => item.itemName == itemName);

        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            inventoryItems.Add(new InventoryItem(itemName, quantity));
        }

        UpdateInventoryUI();
    }

    /// <summary>
    /// 移除物品
    /// </summary>
    public void RemoveItem(string itemName, int quantity = 1)
    {
        InventoryItem item = inventoryItems.Find(i => i.itemName == itemName);

        if (item != null)
        {
            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                inventoryItems.Remove(item);
            }
        }

        UpdateInventoryUI();
    }

    /// <summary>
    /// 获取物品数量
    /// </summary>
    public int GetItemCount(string itemName)
    {
        InventoryItem item = inventoryItems.Find(i => i.itemName == itemName);
        return item != null ? item.quantity : 0;
    }

    /// <summary>
    /// 检查是否有足够的物品
    /// </summary>
    public bool HasEnoughItems(string itemName, int quantity)
    {
        return GetItemCount(itemName) >= quantity;
    }

    /// <summary>
    /// 更新背包UI
    /// </summary>
    public void UpdateInventoryUI()
    {
        if (itemListContent == null) return;

        foreach (Transform child in itemListContent)
        {
            Destroy(child.gameObject);
        }

        int totalCount = 0;
        foreach (InventoryItem item in inventoryItems)
        {
            if (item.quantity <= 0) continue;

            GameObject slotObj = new GameObject("ItemSlot_" + item.itemName);
            slotObj.transform.SetParent(itemListContent);

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 40);

            Image bg = slotObj.AddComponent<Image>();
            bg.color = new Color(0.85f, 0.8f, 0.7f, 1f);

            GameObject nameObj = new GameObject("ItemName");
            nameObj.transform.SetParent(slotObj.transform);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0);
            nameRect.anchorMax = new Vector2(0.7f, 1);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = item.itemName;
            nameText.fontSize = 14f;
            nameText.color = Color.black;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.verticalAlignment = VerticalAlignmentOptions.Middle;

            GameObject qtyObj = new GameObject("ItemQuantity");
            qtyObj.transform.SetParent(slotObj.transform);
            RectTransform qtyRect = qtyObj.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(0.7f, 0);
            qtyRect.anchorMax = new Vector2(0.95f, 1);

            TextMeshProUGUI qtyText = qtyObj.AddComponent<TextMeshProUGUI>();
            qtyText.text = $"x{item.quantity}";
            qtyText.fontSize = 14f;
            qtyText.color = Color.black;
            qtyText.alignment = TextAlignmentOptions.Right;
            qtyText.verticalAlignment = VerticalAlignmentOptions.Middle;

            totalCount += item.quantity;
        }

        if (totalItemsText != null)
            totalItemsText.text = $"{totalCount} items total";
    }

    /// <summary>
    /// 获取所有药草
    /// </summary>
    public List<InventoryItem> GetHerbs()
    {
        List<InventoryItem> herbs = new List<InventoryItem>();
        foreach (InventoryItem item in inventoryItems)
        {
            if (System.Array.IndexOf(herbNames, item.itemName) >= 0 && item.quantity > 0)
            {
                herbs.Add(item);
            }
        }
        return herbs;
    }

    /// <summary>
    /// 获取所有魔药
    /// </summary>
    public List<InventoryItem> GetPotions()
    {
        List<InventoryItem> potions = new List<InventoryItem>();
        foreach (InventoryItem item in inventoryItems)
        {
            if (System.Array.IndexOf(potionNames, item.itemName) >= 0 && item.quantity > 0)
            {
                potions.Add(item);
            }
        }
        return potions;
    }
}

/// <summary>
/// 物品槽UI组件
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI quantityText;
    public TextMeshProUGUI descriptionText;

    public void Setup(InventorySystem.InventoryItem item)
    {
        if (itemNameText != null)
            itemNameText.text = item.itemName;
        if (quantityText != null)
            quantityText.text = $"x{item.quantity}";
        if (descriptionText != null)
            descriptionText.text = item.description;
        if (itemIcon != null && item.itemIcon != null)
            itemIcon.sprite = item.itemIcon;
    }
}
