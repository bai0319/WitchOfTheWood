using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 订单面板系统 - 管理客户订单
/// </summary>
public class OrderPanelSystem : MonoBehaviour
{
    public static OrderPanelSystem instance;

    [System.Serializable]
    public class Order
    {
        public string orderId;              // 订单ID
        public string clientName;           // 客户名称
        [TextArea(1, 3)]
        public string orderDescription;     // 订单描述
        public string requiredItem;         // 需要的物品
        public int quantity;                // 需要数量
        public int currentQuantity;         // 当前交付数量
        public int reward;                  // 奖励金额
        public float timeLimit;             // 时间限制（秒）
        public float remainingTime;         // 剩余时间
        public bool isCompleted;            // 是否完成
        public bool isDelivered;            // 是否已交付

        public Order(string id, string client, string desc, string item, int qty, int rewardAmount, float time)
        {
            orderId = id;
            clientName = client;
            orderDescription = desc;
            requiredItem = item;
            quantity = qty;
            currentQuantity = 0;
            reward = rewardAmount;
            timeLimit = time;
            remainingTime = time;
            isCompleted = false;
            isDelivered = false;
        }

        public float GetProgress()
        {
            if (quantity == 0) return 1f;
            return (float)currentQuantity / quantity;
        }

        public bool CanDeliver()
        {
            return currentQuantity >= quantity && !isDelivered;
        }
    }

    public List<Order> activeOrders = new List<Order>();
    public List<Order> completedOrders = new List<Order>();

    [Header("UI引用")]
    public GameObject orderPanel;
    public Transform orderListContent;      // 订单列表容器
    public TextMeshProUGUI totalOrdersText;  // 订单总数

    [Header("交付提示")]
    public GameObject deliveryNotification;  // 交付成功提示

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
        InitializeDefaultOrders();

        if (orderPanel != null)
            orderPanel.SetActive(false);
    }

    void Update()
    {
        // 更新订单时间
        UpdateOrderTimers();
    }

    /// <summary>
    /// 初始化默认订单
    /// </summary>
    void InitializeDefaultOrders()
    {
        AddOrder(new Order("ORD001", "Villager Hans",
            "Need some Moonlight Grass to treat my sick wife",
            "Moonlight Grass", 3, 50, 300f));

        AddOrder(new Order("ORD002", "Innkeeper",
            "Urgently need Starlight Flowers for a sleeping potion",
            "Starlight Flower", 2, 30, 240f));

        AddOrder(new Order("ORD003", "Mysterious Witch",
            "Collect 5 Mandrakes, generous reward promised",
            "Mandrake", 5, 100, 600f));
    }

    /// <summary>
    /// 添加订单
    /// </summary>
    public void AddOrder(Order order)
    {
        activeOrders.Add(order);
        UpdateOrderUI();
    }

    /// <summary>
    /// 更新订单计时器
    /// </summary>
    void UpdateOrderTimers()
    {
        bool needsUpdate = false;

        foreach (Order order in activeOrders)
        {
            if (!order.isCompleted && !order.isDelivered)
            {
                order.remainingTime -= Time.deltaTime;

                if (order.remainingTime <= 0)
                {
                    order.remainingTime = 0;
                    order.isCompleted = true; // 超时失败
                }

                needsUpdate = true;
            }
        }

        if (needsUpdate)
            UpdateOrderUI();
    }

    /// <summary>
    /// 交付物品给订单
    /// </summary>
    public void DeliverItem(string itemName, int quantity)
    {
        foreach (Order order in activeOrders)
        {
            if (order.requiredItem == itemName && order.CanDeliver())
            {
                int deliverAmount = Mathf.Min(quantity, order.quantity - order.currentQuantity);
                order.currentQuantity += deliverAmount;
                quantity -= deliverAmount;

                if (order.CanDeliver())
                {
                    order.isDelivered = true;
                    ShowDeliveryNotification(order);
                }

                if (quantity <= 0) break;
            }
        }

        UpdateOrderUI();
    }

    /// <summary>
    /// 自动交付背包中匹配的物品
    /// </summary>
    public void AutoDeliverFromInventory()
    {
        if (InventorySystem.instance == null) return;

        foreach (Order order in activeOrders)
        {
            if (order.CanDeliver()) continue;

            string itemName = order.requiredItem;
            int needed = order.quantity - order.currentQuantity;
            int available = InventorySystem.instance.GetItemCount(itemName);

            if (available > 0)
            {
                int deliverAmount = Mathf.Min(needed, available);
                InventorySystem.instance.RemoveItem(itemName, deliverAmount);
                order.currentQuantity += deliverAmount;

                if (order.CanDeliver())
                {
                    order.isDelivered = true;
                    ShowDeliveryNotification(order);
                }
            }
        }

        UpdateOrderUI();
    }

    /// <summary>
    /// 显示交付成功提示
    /// </summary>
    void ShowDeliveryNotification(Order order)
    {
        if (deliveryNotification != null)
        {
            deliveryNotification.SetActive(true);

            TextMeshProUGUI text = deliveryNotification.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"Order [{order.orderId}] completed!\nReward: {order.reward} gold";
            }

            Invoke(nameof(HideDeliveryNotification), 3f);
        }
    }

    void HideDeliveryNotification()
    {
        if (deliveryNotification != null)
            deliveryNotification.SetActive(false);
    }

    /// <summary>
    /// 更新订单UI
    /// </summary>
    public void UpdateOrderUI()
    {
        if (orderListContent == null) return;

        foreach (Transform child in orderListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Order order in activeOrders)
        {
            GameObject slotObj = new GameObject("OrderSlot_" + order.orderId);
            slotObj.transform.SetParent(orderListContent);

            RectTransform rect = slotObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 80);

            Image bg = slotObj.AddComponent<Image>();
            bg.color = order.isCompleted ? new Color(0.8f, 0.9f, 0.8f, 1f) : new Color(0.85f, 0.8f, 0.7f, 1f);

            GameObject clientObj = new GameObject("ClientName");
            clientObj.transform.SetParent(slotObj.transform);
            RectTransform clientRect = clientObj.AddComponent<RectTransform>();
            clientRect.anchorMin = new Vector2(0.05f, 0.65f);
            clientRect.anchorMax = new Vector2(0.95f, 0.95f);

            TextMeshProUGUI clientText = clientObj.AddComponent<TextMeshProUGUI>();
            clientText.text = order.clientName;
            clientText.fontSize = 14f;
            clientText.color = Color.black;
            clientText.alignment = TextAlignmentOptions.Left;
            clientText.fontStyle = FontStyles.Bold;

            GameObject descObj = new GameObject("OrderDesc");
            descObj.transform.SetParent(slotObj.transform);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0.3f);
            descRect.anchorMax = new Vector2(0.95f, 0.6f);

            TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = $"{order.orderDescription} | Need: {order.requiredItem} x{order.quantity}";
            descText.fontSize = 12f;
            descText.color = Color.black;
            descText.alignment = TextAlignmentOptions.Left;

            GameObject statusObj = new GameObject("OrderStatus");
            statusObj.transform.SetParent(slotObj.transform);
            RectTransform statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0);
            statusRect.anchorMax = new Vector2(0.95f, 0.25f);

            TextMeshProUGUI statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = $"Progress: {order.currentQuantity}/{order.quantity} | Reward: {order.reward}pts";
            statusText.fontSize = 12f;
            statusText.color = order.CanDeliver() ? new Color(0.2f, 0.6f, 0.2f) : Color.black;
            statusText.alignment = TextAlignmentOptions.Right;
        }

        if (totalOrdersText != null)
        {
            int active = activeOrders.Count;
            int completed = completedOrders.Count;
            totalOrdersText.text = $"Active: {active} | Completed: {completed}";
        }
    }

    /// <summary>
    /// 获取进行中的订单
    /// </summary>
    public List<Order> GetActiveOrders()
    {
        return activeOrders.FindAll(o => !o.isCompleted && !o.isDelivered);
    }

    /// <summary>
    /// 获取可交付的订单
    /// </summary>
    public List<Order> GetDeliverableOrders()
    {
        return activeOrders.FindAll(o => o.CanDeliver());
    }
}

/// <summary>
/// 订单槽UI组件
/// </summary>
public class OrderSlotUI : MonoBehaviour
{
    public TextMeshProUGUI clientNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI timerText;
    public Image progressBar;
    public GameObject completedBadge;
    public GameObject expiredBadge;

    public void Setup(OrderPanelSystem.Order order)
    {
        if (clientNameText != null)
            clientNameText.text = $"[{order.clientName}]";

        if (descriptionText != null)
            descriptionText.text = order.orderDescription;

        if (progressText != null)
            progressText.text = $"{order.currentQuantity}/{order.quantity} {order.requiredItem}";

        if (progressBar != null)
            progressBar.fillAmount = order.GetProgress();

        // 时间显示
        if (timerText != null)
        {
            if (order.isDelivered)
            {
                timerText.text = "Delivered";
                timerText.color = Color.green;
            }
            else if (order.isCompleted)
            {
                timerText.text = "Expired";
                timerText.color = Color.red;
            }
            else
            {
                int minutes = Mathf.FloorToInt(order.remainingTime / 60);
                int seconds = Mathf.FloorToInt(order.remainingTime % 60);
                timerText.text = $"{minutes:00}:{seconds:00}";
                timerText.color = order.remainingTime < 60 ? Color.red : Color.white;
            }
        }

        // 状态标记
        if (completedBadge != null)
            completedBadge.SetActive(order.isDelivered);
        if (expiredBadge != null)
            expiredBadge.SetActive(order.isCompleted && !order.isDelivered);
    }
}
