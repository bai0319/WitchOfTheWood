using UnityEngine;

/// <summary>
/// 门口传送组件 - 放置在门口交互点上，实现场景切换
/// </summary>
public class DoorTransition : MonoBehaviour, IInteractable
{
    [Header("目标场景设置")]
    public string targetSceneName = "Forest";    // 目标场景名称
    public Vector3 spawnPosition = Vector3.zero; // 玩家在目标场景的出生位置

    [Header("交互设置")]
    public string promptText = "按E进入森林";     // 交互提示文本
    public string dialogText = "你走出了小屋，来到了森林。"; // 切换前的对话

    [Header("视觉设置")]
    public Color highlightColor = new Color(1f, 0.9f, 0.5f, 1f);
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 注册到交互系统
        InteractableHighlighter.instance?.RegisterInteractable(this);
    }

    void OnDestroy()
    {
        InteractableHighlighter.instance?.UnregisterInteractable(this);
    }

    /// <summary>
    /// 执行交互 - 切换到目标场景
    /// </summary>
    public void Interact()
    {
        Debug.Log($"门口交互: 切换到场景 {targetSceneName}");

        // 显示对话
        if (!string.IsNullOrEmpty(dialogText))
        {
            DialogSystem.instance?.StartDialog(dialogText);
        }

        // 延迟切换场景（等对话显示完）
        Invoke(nameof(DoTransition), 1.5f);
    }

    /// <summary>
    /// 执行场景切换
    /// </summary>
    void DoTransition()
    {
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.TransitionToScene(targetSceneName, spawnPosition);
        }
        else
        {
            Debug.LogError("SceneTransitionManager 未找到！请确保场景中有 SceneTransitionManager 对象。");
        }
    }

    /// <summary>
    /// 获取交互提示
    /// </summary>
    public string GetPrompt()
    {
        return promptText;
    }

    /// <summary>
    /// 设置高亮
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (spriteRenderer != null)
        {
            if (highlight)
            {
                spriteRenderer.color = highlightColor;
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
}
