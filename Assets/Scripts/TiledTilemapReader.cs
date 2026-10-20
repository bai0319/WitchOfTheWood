using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Tiled Tilemap属性读取器 - 从Tilemap中读取Tiled自定义属性
/// 支持属性：action_type, interact_text, interactable
/// </summary>
public class TiledTilemapReader : MonoBehaviour
{
    public static TiledTilemapReader instance;

    [Header("Tilemap设置")]
    public Tilemap[] interactableTilemaps;    // 需要读取的Tilemap数组
    public LayerMask tilemapLayer;            // Tilemap所在层

    [Header("交互区域设置")]
    public float interactionRadius = 0.8f;    // 交互区域半径
    public Color debugColor = Color.cyan;     // 调试颜色

    private List<TileInteractData> interactableTiles = new List<TileInteractData>();
    private Dictionary<Vector3Int, TileInteractData> tileDataMap = new Dictionary<Vector3Int, TileInteractData>();

    [System.Serializable]
    public class TileInteractData
    {
        public Vector3Int tilePosition;       // 瓦片坐标
        public Vector3 worldPosition;         // 世界坐标
        public string actionType;             // 交互类型 (book, door, yao等)
        public string interactText;           // 交互文本/线索内容
        public bool interactable;             // 是否可交互
        public GameObject colliderObject;     // 生成的碰撞体对象
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
        // 如果没有指定Tilemap，自动查找场景中的所有Tilemap
        if (interactableTilemaps == null || interactableTilemaps.Length == 0)
        {
            FindAllTilemaps();
        }

        // 读取所有可交互瓦片的属性
        ReadAllTileProperties();
    }

    /// <summary>
    /// 自动查找所有Tilemap
    /// </summary>
    void FindAllTilemaps()
    {
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        interactableTilemaps = allTilemaps;
        Debug.Log($"自动找到 {allTilemaps.Length} 个Tilemap");
    }

    /// <summary>
    /// 读取所有瓦片属性
    /// </summary>
    void ReadAllTileProperties()
    {
        foreach (Tilemap tilemap in interactableTilemaps)
        {
            if (tilemap == null) continue;

            BoundsInt bounds = tilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int tilePos = new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(tilePos);

                    if (tile != null)
                    {
                        ReadTileProperties(tilemap, tilePos, tile);
                    }
                }
            }
        }

        Debug.Log($"读取到 {interactableTiles.Count} 个可交互瓦片");
    }

    /// <summary>
    /// 读取单个瓦片的属性
    /// </summary>
    void ReadTileProperties(Tilemap tilemap, Vector3Int tilePos, TileBase tile)
    {
        TileInteractData data = new TileInteractData();
        data.tilePosition = tilePos;
        data.worldPosition = tilemap.CellToWorld(tilePos) + new Vector3(0.5f, 0.5f, 0); // 中心位置

        // 尝试获取瓦片属性
        if (tile is Tile)
        {
            ReadTilePropertiesFromTile(tile as Tile, data);
        }
        else if (tile is RuleTile)
        {
            ReadTilePropertiesFromRuleTile(tile as RuleTile, data);
        }
        else
        {
            // 其他类型的Tile，尝试通用方式读取
            ReadCustomProperties(tile, data);
        }

        // 如果不可交互，跳过
        // exit 类型自动默认可交互（无需设置 interactable 属性）
        if (data.actionType == "exit")
        {
            data.interactable = true;
        }
        if (!data.interactable) return;

        // 确保actionType不为空
        if (string.IsNullOrEmpty(data.actionType))
        {
            data.actionType = "examine";
        }

        // 添加到列表
        interactableTiles.Add(data);
        tileDataMap[tilePos] = data;

        // 创建碰撞体对象
        CreateInteractionCollider(data);
    }

    /// <summary>
    /// 从Tile读取属性
    /// </summary>
    void ReadTilePropertiesFromTile(Tile tile, TileInteractData data)
    {
        if (tile == null) return;

        if (tile.GetType().GetProperty("action_type") != null)
        {
            data.actionType = tile.GetType().GetProperty("action_type").GetValue(tile) as string;
        }

        if (tile.GetType().GetProperty("interact_text") != null)
        {
            data.interactText = tile.GetType().GetProperty("interact_text").GetValue(tile) as string;
        }

        if (tile.GetType().GetProperty("interactable") != null)
        {
            object value = tile.GetType().GetProperty("interactable").GetValue(tile);
            if (value != null)
            {
                data.interactable = (bool)value;
            }
        }

        // 尝试从自定义数据读取（Tiled导出的自定义属性）
        ReadCustomProperties(tile, data);
    }

    /// <summary>
    /// 从RuleTile读取属性
    /// </summary>
    void ReadTilePropertiesFromRuleTile(RuleTile ruleTile, TileInteractData data)
    {
        ReadCustomProperties(ruleTile, data);
    }

    /// <summary>
    /// 读取自定义属性（通用方法）
    /// </summary>
    void ReadCustomProperties(Object obj, TileInteractData data)
    {
        if (obj == null) return;

        // 使用Unity的序列化系统尝试读取自定义属性
        // Tiled2Unity或类似工具通常会将自定义属性存储在Tile的customData中
        try
        {
            // 尝试通过反射获取自定义数据
            var customDataProp = obj.GetType().GetProperty("customData");
            if (customDataProp != null)
            {
                string customData = customDataProp.GetValue(obj) as string;
                if (!string.IsNullOrEmpty(customData))
                {
                    ParseCustomData(customData, data);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"读取自定义属性失败: {e.Message}");
        }
    }

    /// <summary>
    /// 解析自定义数据字符串
    /// </summary>
    void ParseCustomData(string customData, TileInteractData data)
    {
        // 自定义数据可能是JSON格式或键值对格式
        // 尝试解析各种格式

        // 格式1: JSON格式 {"action_type":"book","interact_text":"线索","interactable":true}
        if (customData.StartsWith("{") && customData.EndsWith("}"))
        {
            try
            {
                // 简单解析JSON
                string actionType = ExtractValue(customData, "action_type");
                string interactText = ExtractValue(customData, "interact_text");
                string interactable = ExtractValue(customData, "interactable");

                if (!string.IsNullOrEmpty(actionType)) data.actionType = actionType;
                if (!string.IsNullOrEmpty(interactText)) data.interactText = interactText;
                if (!string.IsNullOrEmpty(interactable))
                {
                    bool.TryParse(interactable, out bool result);
                    data.interactable = result;
                }
            }
            catch { }
        }
        else
        {
            // 格式2: 键值对格式 action_type=book;interact_text=线索;interactable=true
            string[] pairs = customData.Split(';');
            foreach (string pair in pairs)
            {
                string[] keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim().ToLower();
                    string value = keyValue[1].Trim();

                    if (key == "action_type")
                        data.actionType = value;
                    else if (key == "interact_text")
                        data.interactText = value;
                    else if (key == "interactable")
                        bool.TryParse(value, out data.interactable);
                }
            }
        }
    }

    /// <summary>
    /// 从JSON字符串中提取值
    /// </summary>
    string ExtractValue(string json, string key)
    {
        int startIndex = json.IndexOf($"\"{key}\"");
        if (startIndex == -1) return null;

        int colonIndex = json.IndexOf(':', startIndex);
        int valueStart = json.IndexOfAny(new char[] { '"', 't', 'f', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-' }, colonIndex + 1);

        if (json[valueStart] == '"')
        {
            // 字符串值
            int valueEnd = json.IndexOf('"', valueStart + 1);
            return json.Substring(valueStart + 1, valueEnd - valueStart - 1);
        }
        else
        {
            // 布尔或数字值
            int valueEnd = json.IndexOfAny(new char[] { ',', '}' }, valueStart);
            return json.Substring(valueStart, valueEnd - valueStart);
        }
    }

    /// <summary>
    /// 创建交互碰撞体
    /// </summary>
    void CreateInteractionCollider(TileInteractData data)
    {
        // 创建一个空对象作为交互触发器
        GameObject colliderObj = new GameObject($"Interactable_{data.actionType}_{data.tilePosition}");
        colliderObj.transform.position = data.worldPosition;
        colliderObj.transform.SetParent(transform);

        // 添加碰撞体
        CircleCollider2D collider = colliderObj.AddComponent<CircleCollider2D>();
        collider.radius = interactionRadius;
        collider.isTrigger = true;

        // 添加交互组件
        TiledInteractableObject interactable = colliderObj.AddComponent<TiledInteractableObject>();
        interactable.Setup(data);

        data.colliderObject = colliderObj;
    }

    /// <summary>
    /// 获取指定位置的交互数据
    /// </summary>
    public TileInteractData GetTileData(Vector3Int tilePos)
    {
        if (tileDataMap.ContainsKey(tilePos))
        {
            return tileDataMap[tilePos];
        }
        return null;
    }

    /// <summary>
    /// 获取指定世界位置附近的交互数据
    /// </summary>
    public TileInteractData GetTileDataAtWorldPosition(Vector3 worldPos, Tilemap tilemap)
    {
        Vector3Int tilePos = tilemap.WorldToCell(worldPos);
        return GetTileData(tilePos);
    }

    /// <summary>
    /// 获取所有可交互数据
    /// </summary>
    public List<TileInteractData> GetAllInteractableTiles()
    {
        return interactableTiles;
    }

    /// <summary>
    /// 获取指定类型的交互数据
    /// </summary>
    public List<TileInteractData> GetTilesByActionType(string actionType)
    {
        return interactableTiles.FindAll(t => t.actionType == actionType);
    }

    void OnDrawGizmosSelected()
    {
        // 绘制所有可交互瓦片的位置
        Gizmos.color = debugColor;
        foreach (TileInteractData data in interactableTiles)
        {
            Gizmos.DrawWireSphere(data.worldPosition, interactionRadius);
        }
    }
}

/// <summary>
/// Tiled可交互对象组件 - 挂载到从Tilemap生成的交互区域上
/// </summary>
public class TiledInteractableObject : MonoBehaviour, IInteractable
{
    private TiledTilemapReader.TileInteractData tileData;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public void Setup(TiledTilemapReader.TileInteractData data)
    {
        tileData = data;

        // 获取父Tilemap的SpriteRenderer（如果有）
        Transform parentTilemap = transform.parent;
        if (parentTilemap != null)
        {
            Tilemap tilemap = parentTilemap.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                // 获取瓦片对应的Sprite
                Sprite sprite = tilemap.GetSprite(data.tilePosition);
                if (sprite != null)
                {
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.sortingOrder = tilemap.GetComponent<TilemapRenderer>()?.sortingOrder ?? 0;
                    originalColor = spriteRenderer.color;
                }
            }
        }

        // 注册到高亮系统
        InteractableHighlighter.instance?.RegisterInteractable(this);
    }

    /// <summary>
    /// 执行交互
    /// </summary>
    public void Interact()
    {
        if (tileData == null) return;

        Debug.Log($"交互: {tileData.actionType} - {tileData.interactText}");

        // 根据action_type执行不同的交互行为
        switch (tileData.actionType.ToLower())
        {
            case "book":
                HandleBookInteraction();
                break;
            case "door":
                HandleDoorInteraction();
                break;
            case "exit":
                HandleExitInteraction();
                break;
            case "yao":
                HandleYaoInteraction();
                break;
            case "collect":
                HandleCollectInteraction();
                break;
            case "examine":
            default:
                HandleExamineInteraction();
                break;
        }
    }

    /// <summary>
    /// 书籍交互 - 读取并获取线索物品
    /// </summary>
    void HandleBookInteraction()
    {
        if (!string.IsNullOrEmpty(tileData.interactText))
        {
            DialogSystem.instance?.StartDialog(tileData.interactText);
        }

        // 将书籍内容作为物品添加到背包
        string bookItemName = $"书籍_{tileData.tilePosition}";
        InventorySystem.instance?.AddItem(bookItemName, 1);

        // 添加线索到日记本
        if (!string.IsNullOrEmpty(tileData.interactText))
        {
            ClueDiarySystem.instance?.AddClue($"书籍内容", tileData.interactText);
        }

        DialogSystem.instance?.StartDialog("你获得了书中的知识。");
    }

    /// <summary>
    /// 门交互 - 打开门2秒后自动关闭
    /// </summary>
    void HandleDoorInteraction()
    {
        DialogSystem.instance?.StartDialog("门打开了，你可以通过。");

        // 查找门所在位置的碰撞体并禁用
        TilemapCollider2D tilemapCollider = GetComponentInParent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            // 创建一个临时的碰撞体禁用区域
            DisableTileCollision(tilemapCollider, tileData.tilePosition);
        }

        // 添加线索
        if (!string.IsNullOrEmpty(tileData.interactText))
        {
            ClueDiarySystem.instance?.AddClue($"关于门", tileData.interactText);
        }
    }

    /// <summary>
    /// 出口交互 - 切换到另一个场景（森林）
    /// interact_text 格式: "场景名|出生X|出生Y" 例如 "Forest|0|5"
    /// </summary>
    void HandleExitInteraction()
    {
        string targetScene = "Forest";
        Vector3 spawnPos = Vector3.zero;

        // 解析 interact_text: "场景名|出生X|出生Y"
        if (!string.IsNullOrEmpty(tileData.interactText))
        {
            string[] parts = tileData.interactText.Split('|');
            if (parts.Length >= 1) targetScene = parts[0].Trim();
            if (parts.Length >= 3)
            {
                float.TryParse(parts[1].Trim(), out float x);
                float.TryParse(parts[2].Trim(), out float y);
                spawnPos = new Vector3(x, y, 0);
            }
        }

        Debug.Log($"出口交互: 切换到场景 {targetScene}, 出生点: {spawnPos}");

        DialogSystem.instance?.StartDialog("你走出了小屋，来到了森林。");

        // 延迟切换场景
        Invoke("DoSceneTransition", 1.5f);
    }

    /// <summary>
    /// 执行场景切换（由Invoke调用）
    /// </summary>
    void DoSceneTransition()
    {
        string targetScene = "Forest";
        Vector3 spawnPos = Vector3.zero;

        if (!string.IsNullOrEmpty(tileData.interactText))
        {
            string[] parts = tileData.interactText.Split('|');
            if (parts.Length >= 1) targetScene = parts[0].Trim();
            if (parts.Length >= 3)
            {
                float.TryParse(parts[1].Trim(), out float x);
                float.TryParse(parts[2].Trim(), out float y);
                spawnPos = new Vector3(x, y, 0);
            }
        }

        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.TransitionToScene(targetScene, spawnPos);
        }
        else
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            SceneTransitionManager manager = managerObj.AddComponent<SceneTransitionManager>();
            manager.TransitionToScene(targetScene, spawnPos);
        }
    }

    /// <summary>
    /// 禁用指定瓦片位置的碰撞
    /// </summary>
    void DisableTileCollision(TilemapCollider2D tilemapCollider, Vector3Int tilePos)
    {
        // 禁用整个Tilemap碰撞体2秒
        tilemapCollider.enabled = false;

        // 2秒后恢复碰撞
        Invoke(nameof(EnableTileCollision), 2f);
    }

    /// <summary>
    /// 恢复瓦片碰撞
    /// </summary>
    void EnableTileCollision()
    {
        TilemapCollider2D tilemapCollider = GetComponentInParent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            tilemapCollider.enabled = true;
        }
        DialogSystem.instance?.StartDialog("门关上了。");
    }

    /// <summary>
    /// 药剂台交互 - 打开制作界面
    /// </summary>
    void HandleYaoInteraction()
    {
        DialogSystem.instance?.StartDialog("这是一个药剂制作台。");

        // 打开药剂制作面板
        PotionCraftingSystem.instance?.OpenCraftingPanel();
    }

    /// <summary>
    /// 采集物交互 - 采集草药并添加到背包
    /// </summary>
    void HandleCollectInteraction()
    {
        string itemName = tileData.interactText;
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = "草药";
        }

        DialogSystem.instance?.StartDialog($"你采集了 {itemName}。");

        InventorySystem.instance?.AddItem(itemName, 1);

        if (tileData.colliderObject != null)
        {
            tileData.colliderObject.SetActive(false);
        }

        Debug.Log($"采集成功: {itemName}");
    }

    /// <summary>
    /// 检查交互 - 显示描述
    /// </summary>
    void HandleExamineInteraction()
    {
        if (!string.IsNullOrEmpty(tileData.interactText))
        {
            DialogSystem.instance?.StartDialog(tileData.interactText);

            // 添加线索
            ClueDiarySystem.instance?.AddClue($"关于物件", tileData.interactText);
        }
        else
        {
            DialogSystem.instance?.StartDialog("这是一个普通的物品。");
        }
    }

    /// <summary>
    /// 获取交互提示
    /// </summary>
    public string GetPrompt()
    {
        return $"[按E交互] {GetActionTypeName()}";
    }

    /// <summary>
    /// 获取交互类型名称
    /// </summary>
    string GetActionTypeName()
    {
        switch (tileData.actionType.ToLower())
        {
            case "book": return "书籍";
            case "door": return "门";
            case "exit": return "出口";
            case "yao": return "药剂台";
            case "collect": return "采集物";
            default: return "物品";
        }
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
                spriteRenderer.color = new Color(1f, 0.9f, 0.5f, 1f);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    void OnDestroy()
    {
        InteractableHighlighter.instance?.UnregisterInteractable(this);
    }
}
