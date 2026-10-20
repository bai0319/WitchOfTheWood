using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [Header("移动参数")]
    public float moveSpeed = 0.5f;  // 进一步降低移动速度

    [Header("UI按键绑定")]
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode diaryKey = KeyCode.J;
    public KeyCode orderKey = KeyCode.O;

    private Rigidbody2D rb;
    private Vector2 movement;
    private bool canMove = true;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        Debug.Log("PlayerController started, GameObject: " + gameObject.name + ", Active: " + gameObject.activeSelf);
        
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PhysicsMaterial2D mat = new PhysicsMaterial2D("PlayerMaterial");
        mat.friction = 0f;
        mat.bounciness = 0f;
        rb.sharedMaterial = mat;

        // 检查是否有场景切换的出生位置
        CheckSpawnPosition();
    }

    /// <summary>
    /// 检查场景切换后的出生位置
    /// </summary>
    void CheckSpawnPosition()
    {
        if (SceneTransitionManager.instance != null && SceneTransitionManager.instance.HasSpawnPosition())
        {
            Vector3 spawnPos = SceneTransitionManager.instance.GetSpawnPosition();
            Debug.Log($"Scene transition spawn point: {spawnPos}");
            transform.position = spawnPos;
            SceneTransitionManager.instance.OnTransitionComplete();
        }
    }

    void Update()
    {
        // 检测UI快捷键
        HandleUIInput();

        // 检测是否在对话中
        if (DialogSystem.instance != null && DialogSystem.instance.IsInDialog())
        {
            canMove = false;
            movement = Vector2.zero;
            return;
        }

        canMove = true;

        // 获取移动输入
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // 标准化对角线移动
        if (movement.magnitude > 1)
        {
            movement = movement.normalized;
        }
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        Vector2 newPosition = rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    /// <summary>
    /// 处理UI输入
    /// </summary>
    void HandleUIInput()
    {
        // Check for both UIManager (cabin) and SenlinUIManager (forest)
        if (Input.GetKeyDown(inventoryKey))
        {
            Debug.Log("PlayerController: I key pressed, UIManager.instance=" + (UIManager.instance != null ? "exists" : "null"));
            UIManager.instance?.ToggleInventory();
            SenlinUIManager senlinUI = FindObjectOfType<SenlinUIManager>();
            if (senlinUI != null) senlinUI.ToggleInventory();
        }

        if (Input.GetKeyDown(diaryKey))
        {
            Debug.Log("PlayerController: J key pressed, UIManager.instance=" + (UIManager.instance != null ? "exists" : "null"));
            UIManager.instance?.ToggleClueDiary();
            SenlinUIManager senlinUI = FindObjectOfType<SenlinUIManager>();
            if (senlinUI != null) senlinUI.ToggleClueDiary();
        }

        if (Input.GetKeyDown(orderKey))
        {
            Debug.Log("PlayerController: O key pressed, UIManager.instance=" + (UIManager.instance != null ? "exists" : "null"));
            UIManager.instance?.ToggleOrderPanel();
            SenlinUIManager senlinUI = FindObjectOfType<SenlinUIManager>();
            if (senlinUI != null) senlinUI.ToggleOrderPanel();
        }

        // Escape key for main menu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UIManager.instance?.ToggleMainMenu();
            SenlinUIManager senlinUI = FindObjectOfType<SenlinUIManager>();
            if (senlinUI != null) senlinUI.ToggleMainMenu();
        }
    }

    /// <summary>
    /// 禁止移动
    /// </summary>
    public void DisableMovement()
    {
        canMove = false;
        movement = Vector2.zero;
    }

    /// <summary>
    /// 允许移动
    /// </summary>
    public void EnableMovement()
    {
        canMove = true;
    }

    /// <summary>
    /// 获取当前移动方向
    /// </summary>
    public Vector2 GetMovementDirection()
    {
        return movement;
    }

    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool IsMoving()
    {
        return movement.magnitude > 0;
    }
}
