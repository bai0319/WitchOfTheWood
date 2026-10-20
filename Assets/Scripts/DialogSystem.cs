using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 对话系统 - 底部固定对话框，无需选项分支
/// </summary>
public class DialogSystem : MonoBehaviour
{
    public static DialogSystem instance;

    [Header("对话框UI")]
    public GameObject dialogPanel;
    public TextMeshProUGUI speakerNameText;      // 说话者名称
    public TextMeshProUGUI dialogContentText;   // 对话内容
    public Image speakerPortrait;                // 说话者头像（可选）

    [Header("对话数据")]
    [TextArea(2, 4)]
    public List<string> dialogLines = new List<string>();  // 对话内容列表
    private int currentLineIndex = 0;

    [Header("打字机效果")]
    public float typeSpeed = 0.03f;             // 打字速度
    private bool isTyping = false;
    private string currentFullText = "";

    [Header("继续提示")]
    public GameObject continuePrompt;           // 继续提示图标
    private float continuePromptTimer = 0f;

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
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        if (continuePrompt != null)
            continuePrompt.SetActive(false);
    }

    void Update()
    {
        // 检测继续输入
        if (dialogPanel != null && dialogPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                if (isTyping)
                {
                    // 正在打字时，点击直接显示完整文字
                    CompleteCurrentLine();
                }
                else
                {
                    // 显示下一行
                    ShowNextLine();
                }
            }
        }

        // 继续提示闪烁
        if (continuePrompt != null && continuePrompt.activeSelf)
        {
            continuePromptTimer += Time.deltaTime;
            continuePrompt.SetActive(Mathf.Sin(continuePromptTimer * 4f) > 0);
        }
    }

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialog(List<string> lines, string speakerName = "")
    {
        if (lines == null || lines.Count == 0) return;

        dialogLines = new List<string>(lines);
        currentLineIndex = 0;

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
            if (speakerNameText != null)
                speakerNameText.text = speakerName;
        }

        ShowCurrentLine();
    }

    /// <summary>
    /// 开始单句对话
    /// </summary>
    public void StartDialog(string line, string speakerName = "")
    {
        List<string> lines = new List<string> { line };
        StartDialog(lines, speakerName);
    }

    /// <summary>
    /// 显示当前行
    /// </summary>
    void ShowCurrentLine()
    {
        if (currentLineIndex >= dialogLines.Count)
        {
            EndDialog();
            return;
        }

        currentFullText = dialogLines[currentLineIndex];
        StartCoroutine(TypeText(currentFullText));
    }

    /// <summary>
    /// 显示下一行
    /// </summary>
    public void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < dialogLines.Count)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialog();
        }
    }

    /// <summary>
    /// 打字机效果协程
    /// </summary>
    System.Collections.IEnumerator TypeText(string text)
    {
        isTyping = true;
        if (continuePrompt != null) continuePrompt.SetActive(false);

        if (dialogContentText != null)
        {
            dialogContentText.text = "";
            foreach (char letter in text)
            {
                dialogContentText.text += letter;
                yield return new WaitForSeconds(typeSpeed);
            }
        }

        isTyping = false;
        continuePromptTimer = 0f;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    /// <summary>
    /// 完成当前行的打字效果
    /// </summary>
    void CompleteCurrentLine()
    {
        StopAllCoroutines();
        if (dialogContentText != null)
            dialogContentText.text = currentFullText;
        isTyping = false;
        continuePromptTimer = 0f;
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialog()
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        currentLineIndex = 0;
        dialogLines.Clear();
    }

    /// <summary>
    /// 检查是否正在对话
    /// </summary>
    public bool IsInDialog()
    {
        return dialogPanel != null && dialogPanel.activeSelf;
    }
}
