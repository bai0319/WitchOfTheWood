using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Xml;
using System.IO;

/// <summary>
/// Tiled Object Layer Loader - Reads object layers from TMX files and generates interactable objects
/// </summary>
public class TiledObjectLoader : MonoBehaviour
{
    public static TiledObjectLoader instance;

    [Header("TMX File Path (relative to Assets)")]
    public string tmxFilePath = "Sprites/森林.tmx";

    [Header("Pixels to Unity Units (1/tile size)")]
    public float pixelsPerUnit = 16f;

    [Header("Auto-detect scale from Tilemap (recommended)")]
    public bool autoDetectScaleFromTilemap = true;

    [Header("Manual world scale (used when auto-detect is off)")]
    public float manualWorldScale = 0.01f;

    private float worldScale = 0.01f;

    [Header("Object layer names (supports Chinese/English)")]
    public string[] objectLayerNames = { "门", "植物", "采集物", "交互", "对象层", "exit", "collect", "interact" };

    [Header("Default collider size for point objects (tile units)")]
    public Vector2 defaultColliderSize = new Vector2(1f, 1f);

    [Header("Plant sprite path prefix")]
    public string spritePathPrefix = "Sprites/";

    private List<GameObject> spawnedObjects = new List<GameObject>();

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

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[TiledObjectLoader] Scene loaded: {scene.name}, reloading objects...");
        LoadObjectsForCurrentScene();
    }

    void Start()
    {
        LoadObjectsForCurrentScene();
    }

    /// <summary>
    /// Automatically select TMX file based on current scene name
    /// </summary>
    public void LoadObjectsForCurrentScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string tmxName = GetTMXNameForScene(sceneName);

        if (!string.IsNullOrEmpty(tmxName))
        {
            tmxFilePath = $"Sprites/{tmxName}.tmx";
            Debug.Log($"[TiledObjectLoader] Current scene: {sceneName}, loading TMX: {tmxFilePath}");
        }
        else
        {
            Debug.LogWarning($"[TiledObjectLoader] Scene {sceneName} has no TMX config, using default path: {tmxFilePath}");
        }

        LoadObjectsFromTMX();
    }

    string GetTMXNameForScene(string sceneName)
    {
        string lower = sceneName.ToLower();
        if (lower.Contains("sample") || lower.Contains("hut") || lower.Contains("cabin") || lower.Contains("林中小屋"))
            return "林中小屋";
        if (lower.Contains("senlin") || lower.Contains("forest") || lower.Contains("森林"))
            return "森林";
        return "";
    }

    /// <summary>
    /// Load object layers from TMX file
    /// </summary>
    public void LoadObjectsFromTMX()
    {
        // Clear previous objects when reloading
        foreach (GameObject go in spawnedObjects)
        {
            if (go != null) Destroy(go);
        }
        spawnedObjects.Clear();

        string fullPath = Path.Combine(Application.dataPath, tmxFilePath);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"[TiledObjectLoader] TMX file not found: {fullPath}");
            return;
        }

        XmlDocument doc = new XmlDocument();
        doc.Load(fullPath);

        // Read tile size from map node
        int tileWidth = 16;
        int tileHeight = 16;
        XmlElement mapElem = doc.DocumentElement;
        if (mapElem != null && mapElem.Name == "map")
        {
            string twStr = mapElem.GetAttribute("tilewidth");
            string thStr = mapElem.GetAttribute("tileheight");
            if (int.TryParse(twStr, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int tw) && tw > 0)
            {
                tileWidth = tw;
                pixelsPerUnit = tw;
            }
            if (int.TryParse(thStr, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out int th) && th > 0)
            {
                tileHeight = th;
            }
            Debug.Log($"[TiledObjectLoader] TMX tile size: {tileWidth}x{tileHeight}");
        }

        // Calculate world coordinate scale
        if (autoDetectScaleFromTilemap)
        {
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            if (tilemaps.Length > 0)
            {
                Tilemap tilemap = tilemaps[0];
                float cellSize = tilemap.cellSize.x;
                worldScale = cellSize / tileWidth;
                Debug.Log($"[TiledObjectLoader] Auto-detected scale: cellSize={cellSize}, tileWidth={tileWidth}, worldScale={worldScale:F4}");
            }
            else
            {
                worldScale = 1f / pixelsPerUnit;
                Debug.LogWarning($"[TiledObjectLoader] No Tilemap found, using default scale worldScale={worldScale:F4}");
            }
        }
        else
        {
            worldScale = manualWorldScale;
            Debug.Log($"[TiledObjectLoader] Using manual scale worldScale={worldScale:F4}");
        }

        XmlNodeList objectGroups = doc.GetElementsByTagName("objectgroup");
        int totalObjects = 0;

        foreach (XmlNode objectGroup in objectGroups)
        {
            string layerName = GetAttribute(objectGroup, "name");

            bool isTargetLayer = false;
            foreach (string name in objectLayerNames)
            {
                if (layerName.Contains(name) || name.Contains(layerName))
                {
                    isTargetLayer = true;
                    break;
                }
            }

            if (!isTargetLayer) continue;

            XmlNodeList objects = objectGroup.SelectNodes("object");
            Debug.Log($"[TiledObjectLoader] Loading object layer [{layerName}], {objects.Count} objects found");

            foreach (XmlNode obj in objects)
            {
                CreateInteractableObject(obj);
                totalObjects++;
            }
        }

        Debug.Log($"[TiledObjectLoader] Loading complete, created {totalObjects} interactable objects");
    }

    /// <summary>
    /// Create an interactable object
    /// </summary>
    void CreateInteractableObject(XmlNode objNode)
    {
        float x = GetFloatAttribute(objNode, "x");
        float y = GetFloatAttribute(objNode, "y");
        float width = GetFloatAttribute(objNode, "width");
        float height = GetFloatAttribute(objNode, "height");
        string objName = GetAttribute(objNode, "name");
        string objType = GetAttribute(objNode, "type");

        // Point objects (no width/height) use default size
        bool isPointObject = width <= 0.001f || height <= 0.001f;
        if (isPointObject)
        {
            width = pixelsPerUnit;
            height = pixelsPerUnit;
        }

        // Convert Tiled coordinates (pixels) to Unity world coordinates
        // worldScale = Tilemap.cellSize.x / tileWidth
        float unityX = x * worldScale + width * worldScale * 0.5f;
        float unityY = -y * worldScale - height * worldScale * 0.5f;

        // Read custom properties
        string actionType = "";
        string interactText = "";
        string targetScene = "";
        bool interactable = true; // Default to interactable

        XmlNode propertiesNode = objNode.SelectSingleNode("properties");
        if (propertiesNode != null)
        {
            XmlNodeList properties = propertiesNode.SelectNodes("property");
            foreach (XmlNode prop in properties)
            {
                string propName = GetAttribute(prop, "name").ToLower();
                string propValue = GetAttribute(prop, "value");
                string propType = GetAttribute(prop, "type");

                switch (propName)
                {
                    case "action_type":
                        actionType = propValue;
                        break;
                    case "interact_text":
                        interactText = propValue;
                        break;
                    case "target_scene":
                        targetScene = propValue;
                        break;
                    case "interactable":
                        if (propType == "bool")
                        {
                            bool.TryParse(propValue, out interactable);
                        }
                        else
                        {
                            interactable = propValue == "1" || propValue.ToLower() == "true";
                        }
                        break;
                }
            }
        }

        // If action_type is not set, infer from object type or name
        if (string.IsNullOrEmpty(actionType))
        {
            if (!string.IsNullOrEmpty(objType))
                actionType = objType;
            else if (!string.IsNullOrEmpty(objName))
                actionType = objName;
        }

        if (string.IsNullOrEmpty(actionType)) return;

        // Create game object
        GameObject go = new GameObject($"Interactable_{actionType}_{objName}");
        go.transform.position = new Vector3(unityX, unityY, 0);
        go.transform.SetParent(transform);

        // Add collider - use actual object size or default size
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        float colW = width * worldScale;
        float colH = height * worldScale;
        // Ensure collider has minimum size
        if (colW < 0.3f) colW = defaultColliderSize.x * worldScale * pixelsPerUnit;
        if (colH < 0.3f) colH = defaultColliderSize.y * worldScale * pixelsPerUnit;
        col.size = new Vector2(colW, colH);

        // Add visible sprite (if it's a plant)
        if (actionType.ToLower() == "collect" || actionType.ToLower() == "植物")
        {
            LoadPlantSprite(go, objName);
        }

        // Add interaction component
        TiledObjectInteractable interactableComp = go.AddComponent<TiledObjectInteractable>();
        interactableComp.actionType = actionType;
        interactableComp.interactText = interactText;
        interactableComp.interactable = interactable;
        interactableComp.objectName = objName;
        interactableComp.targetScene = targetScene;

        // Register to highlight system
        InteractableHighlighter.instance?.RegisterInteractable(interactableComp);

        spawnedObjects.Add(go);

        Debug.Log($"[TiledObjectLoader] Created interactable object: action={actionType}, name={objName}, pos=({unityX:F2}, {unityY:F2}), collider=({colW:F2}x{colH:F2})");
    }

    /// <summary>
    /// Load plant sprite
    /// </summary>
    void LoadPlantSprite(GameObject go, string plantName)
    {
        if (string.IsNullOrEmpty(plantName)) return;

        Sprite sprite = null;

        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
        foreach (Tilemap tm in tilemaps)
        {
            Vector3Int cellPos = tm.WorldToCell(go.transform.position);
            TileBase tile = tm.GetTile(cellPos);
            if (tile != null)
            {
                Sprite tileSprite = tm.GetSprite(cellPos);
                if (tileSprite != null)
                {
                    sprite = tileSprite;
                    break;
                }
            }
        }

        if (sprite == null)
        {
            string spriteName = GetSpriteNameForPlant(plantName);
            if (!string.IsNullOrEmpty(spriteName))
            {
                sprite = Resources.Load<Sprite>(spritePathPrefix + spriteName);
                if (sprite == null)
                {
                    sprite = Resources.Load<Sprite>(spriteName);
                }
            }
        }

        if (sprite != null)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 10;
            Debug.Log($"[TiledObjectLoader] Loaded plant sprite: {plantName}");
        }
        else
        {
            Debug.LogWarning($"[TiledObjectLoader] Plant sprite not found: {plantName}");
        }
    }

    /// <summary>
    /// Get sprite resource name from plant name
    /// </summary>
    string GetSpriteNameForPlant(string plantName)
    {
        string lower = plantName.ToLower().Trim();
        switch (lower)
        {
            case "sage":
            case "鼠尾草":
            case "bailixiang":
                return "bailixiang";
            case "lavender":
            case "薰衣草":
            case "xunyicao":
                return "xunyicao";
            case "thyme":
            case "百里香":
                return "bailixiang"; // Use same image or replace later
            case "calendula":
            case "金盏花":
            case "jinzhanhua":
                return "jinzhanhua";
            case "nettle":
            case "荨麻":
            case "qianma":
                return "qianma";
            case "comfrey":
            case "康复草":
            case "kangfucao":
                return "kangfucao";
            case "willow":
            case "willow bark":
            case "柳树皮":
                return "bailiushu";
            case "iris":
            case "yellow iris":
            case "黄花鸢尾":
                return "Sprite-0002";
            default:
                return lower;
        }
    }

    string GetAttribute(XmlNode node, string name)
    {
        if (node is XmlElement elem)
            return elem.GetAttribute(name);
        XmlAttribute attr = node.Attributes[name];
        return attr != null ? attr.Value : "";
    }

    float GetFloatAttribute(XmlNode node, string name)
    {
        string val = GetAttribute(node, name);
        float result = 0f;
        float.TryParse(val, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out result);
        return result;
    }
}

/// <summary>
/// Tiled Object Interactable Component
/// </summary>
public class TiledObjectInteractable : MonoBehaviour, IInteractable
{
    public string actionType = "";
    public string interactText = "";
    public bool interactable = false;
    public string objectName = "";
    public string targetScene = "";

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Interact()
    {
        if (!interactable) return;

        Debug.Log($"[TiledObjectInteractable] Interact: action={actionType}, name={objectName}, text={interactText}");

        switch (actionType.ToLower())
        {
            case "exit":
                HandleExit();
                break;
            case "door":
                HandleDoor();
                break;
            case "collect":
            case "植物":
                HandleCollect();
                break;
            case "examine":
            default:
                HandleExamine();
                break;
        }
    }

    void HandleExit()
    {
        string sceneName = targetScene;
        Vector3 spawnPos = Vector3.zero;

        if (string.IsNullOrEmpty(sceneName) && !string.IsNullOrEmpty(interactText))
        {
            string[] parts = interactText.Split('|');
            if (parts.Length >= 1) sceneName = parts[0].Trim();
            if (parts.Length >= 3)
            {
                float.TryParse(parts[1].Trim(), out float x);
                float.TryParse(parts[2].Trim(), out float y);
                spawnPos = new Vector3(x, y, 0);
            }
        }

        sceneName = ResolveSceneName(sceneName);

        if (string.IsNullOrEmpty(sceneName))
        {
            DialogSystem.instance?.StartDialog("This path seems blocked...");
            return;
        }

        DialogSystem.instance?.StartDialog("You left this place.");

        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.TransitionToScene(sceneName, spawnPos);
        }
        else
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            SceneTransitionManager manager = managerObj.AddComponent<SceneTransitionManager>();
            manager.TransitionToScene(sceneName, spawnPos);
        }
    }

    void HandleDoor()
    {
        string sceneName = ResolveSceneName(targetScene);
        Vector3 spawnPos = Vector3.zero;

        Debug.Log($"[TiledObjectInteractable] HandleDoor: targetScene='{targetScene}', interactText='{interactText}', resolved='{sceneName}', currentScene='{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}'");

        // If no target_scene, try to infer from interact_text
        if (string.IsNullOrEmpty(sceneName) && !string.IsNullOrEmpty(interactText))
        {
            sceneName = ResolveSceneName(interactText);

            // Handle common keywords like "out", "exit", "leave"
            string lowerText = interactText.ToLower().Trim();
            if (string.IsNullOrEmpty(sceneName))
            {
                if (lowerText.Contains("exit") || lowerText.Contains("leave") ||
                    lowerText.Contains("离开") || lowerText.Contains("进入小屋") || lowerText.Contains("去小屋") ||
                    lowerText.Contains("小屋"))
                {
                    sceneName = "SampleScene";
                }
                else if (lowerText.Contains("进入森林") || lowerText.Contains("去森林") || lowerText.Contains("回森林") ||
                         lowerText.Contains("go to forest") || lowerText.Contains("outdoor") ||
                         lowerText.Contains("森林"))
                {
                    sceneName = "senlin";
                }
            }
        }

        // Final fallback: if still no scene, infer from current scene
        if (string.IsNullOrEmpty(sceneName))
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
            if (currentScene.Contains("sample") || currentScene.Contains("hut") || currentScene.Contains("cabin") || currentScene.Contains("小屋"))
            {
                sceneName = "senlin";
            }
            else if (currentScene.Contains("senlin") || currentScene.Contains("forest") || currentScene.Contains("森林"))
            {
                sceneName = "SampleScene";
            }
        }

        // Parse spawn position from interact_text if it contains "|"
        if (!string.IsNullOrEmpty(interactText) && interactText.Contains("|"))
        {
            string[] parts = interactText.Split('|');
            if (parts.Length >= 3)
            {
                float.TryParse(parts[1].Trim(), out float x);
                float.TryParse(parts[2].Trim(), out float y);
                spawnPos = new Vector3(x, y, 0);
            }
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"[TiledObjectInteractable] Door transition to: {sceneName}");
            DialogSystem.instance?.StartDialog("The door opens...");

            if (SceneTransitionManager.instance != null)
            {
                SceneTransitionManager.instance.TransitionToScene(sceneName, spawnPos);
            }
            else
            {
                GameObject managerObj = new GameObject("SceneTransitionManager");
                SceneTransitionManager manager = managerObj.AddComponent<SceneTransitionManager>();
                manager.TransitionToScene(sceneName, spawnPos);
            }
        }
        else
        {
            Debug.LogWarning($"[TiledObjectInteractable] Cannot determine target scene. targetScene='{targetScene}', interactText='{interactText}'");
            if (!string.IsNullOrEmpty(interactText))
                DialogSystem.instance?.StartDialog(interactText);
            else
                DialogSystem.instance?.StartDialog("The door is locked.");
        }
    }

    string ResolveSceneName(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        string lower = input.ToLower().Trim();

        if (lower.Contains("senlin") || lower.Contains("forest") || lower.Contains("森林"))
            return "senlin";
        if (lower.Contains("hut") || lower.Contains("cabin") || lower.Contains("林中小屋") || lower.Contains("小屋"))
            return "SampleScene";
        if (lower.Contains("sample"))
            return "SampleScene";

        return "";
    }

    void HandleCollect()
    {
        string itemName = objectName;
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = interactText;
        }
        if (string.IsNullOrEmpty(itemName))
        {
            itemName = "Herb";
        }

        string displayName = GetHerbDisplayName(itemName);
        DialogSystem.instance?.StartDialog($"You collected {displayName}.");
        InventorySystem.instance?.AddItem(displayName, 1);
        gameObject.SetActive(false);
    }

    string GetHerbDisplayName(string rawName)
    {
        string lowerName = rawName.ToLower().Trim();
        switch (lowerName)
        {
            case "sage": case "鼠尾草": case "bailixiang": case "百里香": case "thyme":
                return "Sage";
            case "lavender": case "薰衣草": case "xunyicao":
                return "Lavender";
            case "calendula": case "金盏花": case "jinzhanhua":
                return "Calendula";
            case "nettle": case "荨麻": case "qianma":
                return "Nettle";
            case "comfrey": case "康复草": case "kangfucao":
                return "Comfrey";
            case "willow": case "willow bark": case "柳树皮": case "bailiushu":
                return "Willow Bark";
            case "iris": case "yellow iris": case "黄花鸢尾": case "sprite-0002":
                return "Yellow Iris";
            case "chamomile": case "洋甘菊":
                return "Chamomile";
            default:
                return rawName;
        }
    }

    void HandleExamine()
    {
        if (!string.IsNullOrEmpty(interactText))
        {
            DialogSystem.instance?.StartDialog(interactText);
        }
        else
        {
            DialogSystem.instance?.StartDialog("Nothing special here.");
        }
    }

    public string GetPrompt()
    {
        return $"[E] {GetTypeName()}";
    }

    string GetTypeName()
    {
        switch (actionType.ToLower())
        {
            case "exit": return "Exit";
            case "door": return "Door";
            case "collect":
            case "植物":
                return "Collect";
            default: return "Object";
        }
    }

    public void SetHighlight(bool highlight)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlight ? new Color(1f, 0.9f, 0.5f, 1f) : Color.white;
        }
    }
}