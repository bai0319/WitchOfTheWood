using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 可交互物品基类 - 挂载到可交互的物体上
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("物品信息")]
    public string objectName = "未知物品";      // 物品名称
    public string interactionPrompt = "按E交互"; // 交互提示
    [TextArea(2, 5)]
    public string clueText = "";                  // 交互后获得的线索文本

    [Header("交互类型")]
    public InteractType interactionType = InteractType.Examine;

    [Header("交互效果")]
    public GameObject[] itemsToGive;              // 交互后给出的物品
    public string[] dialogLines;                  // 交互时的对话
    public bool destroyAfterInteraction = false; // 交互后销毁

    [Header("视觉反馈")]
    public Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f); // 高亮颜色
    public float highlightIntensity = 1.2f;

    protected SpriteRenderer spriteRenderer;
    protected Color originalColor;

    public enum InteractType
    {
        Examine,        // 检查/观察
        Pickup,         // 拾取
        Talk,           // 对话
        Open,           // 打开
        Use             // 使用
    }

    protected virtual void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 注册到交互系统
        InteractableHighlighter.instance?.RegisterInteractable(this);
    }

    protected virtual void OnDestroy()
    {
        // 从交互系统注销
        InteractableHighlighter.instance?.UnregisterInteractable(this);
    }

    /// <summary>
    /// 执行交互
    /// </summary>
    public virtual void Interact()
    {
        Debug.Log($"交互：{objectName}");

        // 显示对话
        if (dialogLines != null && dialogLines.Length > 0)
        {
            DialogSystem.instance?.StartDialog(new List<string>(dialogLines), UIManager.instance?.playerName ?? "艾比盖尔");
        }

        // 给物品
        GiveItems();

        // 添加线索到日记本
        if (!string.IsNullOrEmpty(clueText))
        {
            ClueDiarySystem.instance?.AddClueFromInteraction(this);
        }

        // 销毁物体
        if (destroyAfterInteraction)
        {
            Destroy(gameObject);
        }

        // 播放交互音效（如果有）
        PlayInteractionSound();
    }

    /// <summary>
    /// 给予物品
    /// </summary>
    protected virtual void GiveItems()
    {
        if (itemsToGive == null) return;

        foreach (GameObject item in itemsToGive)
        {
            if (item != null)
            {
                // 添加到背包
                string itemName = item.name;
                InventorySystem.instance?.AddItem(itemName, 1);
                Debug.Log($"获得物品：{itemName}");
            }
        }
    }

    /// <summary>
    /// 播放交互音效
    /// </summary>
    protected virtual void PlayInteractionSound()
    {
        // TODO: 播放交互音效
    }

    /// <summary>
    /// 获取交互提示文本
    /// </summary>
    public string GetPrompt()
    {
        return $"[{interactionPrompt}] {objectName}";
    }

    /// <summary>
    /// 设置高亮
    /// </summary>
    public virtual void SetHighlight(bool highlight)
    {
        if (spriteRenderer != null)
        {
            if (highlight)
            {
                spriteRenderer.color = highlightColor * highlightIntensity;
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
}

/// <summary>
/// 可拾取物品 - 继承自InteractableObject
/// </summary>
public class PickupItem : InteractableObject
{
    [Header("拾取物品配置")]
    public string itemNameToGive = "物品";  // 要给的物品名称
    public int quantity = 1;               // 数量

    protected override void GiveItems()
    {
        InventorySystem.instance?.AddItem(itemNameToGive, quantity);
    }
}

/// <summary>
/// 对话物品 - 继承自InteractableObject
/// </summary>
public class TalkableObject : InteractableObject
{
    [Header("对话配置")]
    public string speakerName = "???";
    public bool repeatable = false;         // 是否可以重复对话
    private bool hasTalked = false;

    public override void Interact()
    {
        if (!repeatable && hasTalked)
        {
            // 已经对话过且不可重复
            DialogSystem.instance?.StartDialog("我们已经说过了。", speakerName);
            return;
        }

        hasTalked = true;
        base.Interact();

        // 更新说话者名称
        if (DialogSystem.instance != null && dialogLines != null)
        {
            DialogSystem.instance.StartDialog(new List<string>(dialogLines), speakerName);
        }
    }
}
