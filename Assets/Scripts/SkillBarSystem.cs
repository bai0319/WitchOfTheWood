using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 技能图标栏系统 - 第三阶段解锁
/// </summary>
public class SkillBarSystem : MonoBehaviour
{
    public static SkillBarSystem instance;

    [System.Serializable]
    public class Skill
    {
        public string skillName;           // 技能名称
        [TextArea(1, 3)]
        public string description;         // 技能描述
        public KeyCode activationKey;      // 激活按键
        public Sprite skillIcon;           // 技能图标
        public float cooldownTime;         // 冷却时间
        public float currentCooldown;       // 当前冷却
        public int staminaCost;            // 体力消耗
        public bool isUnlocked;            // 是否已解锁

        public Skill(string name, KeyCode key, float cooldown, int stamina, string desc = "")
        {
            skillName = name;
            activationKey = key;
            cooldownTime = cooldown;
            currentCooldown = 0f;
            staminaCost = stamina;
            description = desc;
            isUnlocked = false;
        }
    }

    public Skill[] skills;

    [Header("UI引用")]
    public GameObject skillBarPanel;
    public Transform skillContainer;        // 技能图标容器
    public GameObject skillSlotPrefab;      // 技能槽预制体

    [Header("体力系统")]
    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaRegenRate = 5f;     // 每秒体力恢复
    public TextMeshProUGUI staminaText;     // 体力文本
    public Image staminaBar;                 // 体力条

    [Header("阶段控制")]
    public int currentPhase = 1;            // 当前阶段

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
        InitializeSkills();

        if (skillBarPanel != null)
            skillBarPanel.SetActive(false);

        currentStamina = maxStamina;
    }

    void Update()
    {
        // 体力恢复
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            UpdateStaminaUI();
        }

        // 更新冷却
        UpdateSkillCooldowns();

        // 检测技能按键
        CheckSkillInput();

        // 更新体力UI
        UpdateStaminaUI();
    }

    /// <summary>
    /// 初始化技能
    /// </summary>
    void InitializeSkills()
    {
        skills = new Skill[]
        {
            new Skill("鸟群探路", KeyCode.Q, 10f, 0, "吹哨呼唤鸟群，触发探路动画，显示前方路况情报"),
            new Skill("驱散狼群", KeyCode.F, 15f, 25, "驱散狼群，消耗体力值，触发驱散判定")
        };
    }

    /// <summary>
    /// 更新技能冷却
    /// </summary>
    void UpdateSkillCooldowns()
    {
        foreach (Skill skill in skills)
        {
            if (skill.currentCooldown > 0)
            {
                skill.currentCooldown -= Time.deltaTime;
                if (skill.currentCooldown < 0)
                    skill.currentCooldown = 0;
            }
        }
    }

    /// <summary>
    /// 检测技能按键输入
    /// </summary>
    void CheckSkillInput()
    {
        if (skillBarPanel == null || !skillBarPanel.activeSelf)
            return;

        foreach (Skill skill in skills)
        {
            if (!skill.isUnlocked) continue;
            if (skill.currentCooldown > 0) continue;

            if (Input.GetKeyDown(skill.activationKey))
            {
                TryActivateSkill(skill);
            }
        }
    }

    /// <summary>
    /// 尝试激活技能
    /// </summary>
    bool TryActivateSkill(Skill skill)
    {
        if (!skill.isUnlocked)
        {
            Debug.Log($"技能「{skill.skillName}」未解锁");
            return false;
        }

        if (currentStamina < skill.staminaCost)
        {
            Debug.Log($"体力不足，无法使用「{skill.skillName}」");
            return false;
        }

        // 消耗体力
        currentStamina -= skill.staminaCost;
        UpdateStaminaUI();

        // 激活技能效果
        ActivateSkillEffect(skill);

        // 开始冷却
        skill.currentCooldown = skill.cooldownTime;

        return true;
    }

    /// <summary>
    /// 激活技能效果
    /// </summary>
    void ActivateSkillEffect(Skill skill)
    {
        Debug.Log($"激活技能：{skill.skillName}");

        switch (skill.skillName)
        {
            case "鸟群探路":
                ActivateBirdScout();
                break;
            case "驱散狼群":
                ActivateWolfDispel();
                break;
        }
    }

    /// <summary>
    /// 鸟群探路技能
    /// </summary>
    void ActivateBirdScout()
    {
        // TODO: 实现鸟群探路动画和路况显示
        Debug.Log("鸟群飞向远方，探查前方路况...");
    }

    /// <summary>
    /// 驱散狼群技能
    /// </summary>
    void ActivateWolfDispel()
    {
        // TODO: 实现驱散狼群判定和动画
        Debug.Log("发出神秘咒语，驱散周围的狼群...");

        // 驱散判定 - 以角色为中心一定范围内的狼群
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            PlayerController.instance.transform.position, 5f);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Wolf"))
            {
                // 驱散狼
                Debug.Log($"驱散了狼群：{col.gameObject.name}");
                // 播放驱散动画/效果
            }
        }
    }

    /// <summary>
    /// 解锁第三阶段技能
    /// </summary>
    public void UnlockPhase3Skills()
    {
        currentPhase = 3;

        if (skills != null && skills.Length >= 2)
        {
            skills[0].isUnlocked = true; // 鸟群探路
            skills[1].isUnlocked = true; // 驱散狼群
        }

        if (skillBarPanel != null)
            skillBarPanel.SetActive(true);

        Debug.Log("第三阶段技能已解锁！");
    }

    /// <summary>
    /// 更新体力UI
    /// </summary>
    void UpdateStaminaUI()
    {
        if (staminaText != null)
            staminaText.text = $"体力: {Mathf.RoundToInt(currentStamina)}/{maxStamina}";

        if (staminaBar != null)
            staminaBar.fillAmount = currentStamina / maxStamina;
    }

    /// <summary>
    /// 获取技能冷却比例
    /// </summary>
    public float GetSkillCooldownRatio(Skill skill)
    {
        if (skill.cooldownTime == 0) return 1f;
        return 1f - (skill.currentCooldown / skill.cooldownTime);
    }

    /// <summary>
    /// 检查是否可以使用的技能
    /// </summary>
    public bool CanUseSkill(Skill skill)
    {
        if (!skill.isUnlocked) return false;
        if (skill.currentCooldown > 0) return false;
        if (currentStamina < skill.staminaCost) return false;
        return true;
    }
}

/// <summary>
/// 技能槽UI组件
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    public Image skillIcon;
    public Image cooldownOverlay;      // 冷却覆盖层
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI staminaCostText;
    public Image lockedOverlay;

    private SkillBarSystem.Skill currentSkill;
    private SkillBarSystem skillSystem;

    public void Setup(SkillBarSystem.Skill skill, SkillBarSystem system)
    {
        currentSkill = skill;
        skillSystem = system;

        if (skillIcon != null && skill.skillIcon != null)
            skillIcon.sprite = skill.skillIcon;

        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (currentSkill == null) return;

        // 更新冷却覆盖
        if (cooldownOverlay != null)
        {
            float ratio = skillSystem != null
                ? skillSystem.GetSkillCooldownRatio(currentSkill)
                : 1f;
            cooldownOverlay.fillAmount = 1f - ratio;
        }

        // 更新冷却文本
        if (cooldownText != null)
        {
            cooldownText.text = currentSkill.currentCooldown > 0
                ? Mathf.RoundToInt(currentSkill.currentCooldown).ToString()
                : "";
        }

        // 更新体力消耗
        if (staminaCostText != null)
            staminaCostText.text = currentSkill.staminaCost > 0
                ? currentSkill.staminaCost.ToString()
                : "";

        // 更新锁定状态
        if (lockedOverlay != null)
            lockedOverlay.gameObject.SetActive(!currentSkill.isUnlocked);
    }
}
