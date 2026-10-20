using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换管理器 - 跨场景持久化，管理地图切换和出生点
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager instance;

    [Header("出生点设置")]
    public Vector3 spawnPosition;          // 目标场景的出生位置
    public string targetSceneName;         // 目标场景名称

    private bool isTransitioning = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SceneTransitionManager created (persistent across scenes)");

            // Clean up any leftover Player objects in DontDestroyOnLoad from previous sessions
            CleanUpLingeringPlayers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Clean up Player objects that may have been incorrectly placed in DontDestroyOnLoad
    /// </summary>
    void CleanUpLingeringPlayers()
    {
        // Find all GameObjects in DontDestroyOnLoad scene
        GameObject dontDestroyObj = GameObject.Find("DontDestroyOnLoad");
        if (dontDestroyObj == null) return;

        // Find and remove any Player objects
        Transform[] children = dontDestroyObj.GetComponentsInChildren<Transform>(true);
        int removed = 0;
        foreach (Transform t in children)
        {
            if (t == null || t.gameObject == null) continue;
            if (t.gameObject == dontDestroyObj) continue;

            // Check if this is a Player object
            bool isPlayer = t.gameObject.name == "Player" ||
                           t.gameObject.CompareTag("Player") ||
                           t.GetComponent<PlayerController>() != null;

            if (isPlayer)
            {
                Debug.LogWarning($"[Cleanup] Removing lingering Player from DontDestroyOnLoad: {t.gameObject.name}");
                Destroy(t.gameObject);
                removed++;
            }
        }

        if (removed > 0)
        {
            Debug.LogWarning($"[Cleanup] Removed {removed} lingering Player object(s) from DontDestroyOnLoad");
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}, spawn: {spawnPosition}");
        LocatePlayerToSpawnPoint();
    }

    void LocatePlayerToSpawnPoint()
    {
        if (string.IsNullOrEmpty(targetSceneName)) return;

        // If no spawn position was specified (Vector3.zero), auto-find the door in the new scene
        if (spawnPosition == Vector3.zero)
        {
            spawnPosition = FindDoorSpawnPosition();
            Debug.Log($"Auto-detected door spawn position: {spawnPosition}");
        }

        // Use FindObjectOfType to get the CURRENT player in the new scene
        // (PlayerController.instance might still point to the OLD destroyed player)
        PlayerController pc = FindObjectOfType<PlayerController>();
        GameObject playerObj = pc != null ? pc.gameObject : GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            playerObj.transform.position = spawnPosition;
            Debug.Log($"Player positioned at spawn: {spawnPosition}");

            // Force re-enable movement on the NEW player
            if (pc == null) pc = playerObj.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.EnableMovement();
                Debug.Log("Player movement re-enabled after scene transition");
            }
        }
        else
        {
            Debug.LogWarning("Player object not found in new scene!");
        }

        // Also clear InteractableHighlighter's mouse mode that may have stuck
        InteractableHighlighter ih = FindObjectOfType<InteractableHighlighter>();
        if (ih == null && InteractableHighlighter.instance != null)
        {
            // The InteractableHighlighter is in DontDestroyOnLoad, so FindObjectOfType won't find it
            // Force enable movement on whatever player it might know about
            if (PlayerController.instance != null)
            {
                PlayerController.instance.EnableMovement();
            }
        }

        OnTransitionComplete();
    }

    /// <summary>
    /// Auto-find a door in the current scene to use as spawn position
    /// </summary>
    Vector3 FindDoorSpawnPosition()
    {
        // 1. Search for TiledObjectInteractable with action_type = door
        TiledObjectInteractable[] tiledDoors = FindObjectsOfType<TiledObjectInteractable>();
        foreach (var d in tiledDoors)
        {
            if (d != null && d.actionType.ToLower() == "door")
            {
                return d.transform.position + new Vector3(0, -0.8f, 0);
            }
        }

        // 2. Default spawn positions based on scene name (use this if no Tiled door found)
        string currentScene = SceneManager.GetActiveScene().name.ToLower();
        if (currentScene.Contains("senlin") || currentScene.Contains("forest"))
        {
            return new Vector3(6.5f, -2f, 0f);
        }
        else if (currentScene.Contains("sample") || currentScene.Contains("cabin") || currentScene.Contains("hut"))
        {
            return new Vector3(6.5f, -2f, 0f);
        }

        // Fallback: return zero
        return Vector3.zero;
    }

    /// <summary>
    /// 切换到目标场景
    /// </summary>
    /// <param name="sceneName">目标场景名称</param>
    /// <param name="spawnPos">玩家出生位置</param>
    public void TransitionToScene(string sceneName, Vector3 spawnPos)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        targetSceneName = sceneName;
        spawnPosition = spawnPos;

        Debug.Log($"Scene transition: {SceneManager.GetActiveScene().name} -> {sceneName}, spawn: {spawnPos}");

        // 禁用玩家移动
        if (PlayerController.instance != null)
        {
            PlayerController.instance.DisableMovement();
        }

        // 加载场景
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 获取出生位置（供PlayerController调用）
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return spawnPosition;
    }

    /// <summary>
    /// 是否有待处理的出生位置
    /// </summary>
    public bool HasSpawnPosition()
    {
        return !string.IsNullOrEmpty(targetSceneName);
    }

    /// <summary>
    /// 切换完成，重置状态
    /// </summary>
    public void OnTransitionComplete()
    {
        isTransitioning = false;
        targetSceneName = "";
    }
}
