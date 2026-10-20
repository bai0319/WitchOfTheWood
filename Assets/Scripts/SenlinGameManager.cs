using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Senlin Scene Game Manager - Independent game manager for the senlin (forest) scene
/// Manages player state, scene loading, and other scene-specific initialization
/// </summary>
public class SenlinGameManager : MonoBehaviour
{
    public static SenlinGameManager instance;

    [Header("Scene Settings")]
    public string currentSceneName = "senlin";
    public string cabinSceneName = "SampleScene";

    [Header("Player State")]
    public Vector3 playerSpawnPosition = Vector3.zero;
    public bool hasSpawnPosition = false;

    [Header("Senlin Scene Config")]
    public string tmxFilePath = "Sprites/森林.tmx";
    public bool autoCreateTiledObjectLoader = true;

    private bool isInitialized = false;

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
            return;
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

    void Start()
    {
        InitializeSenlinScene();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SenlinGameManager] Scene loaded: {scene.name}");
        currentSceneName = scene.name;

        if (scene.name.ToLower().Contains("senlin") || scene.name.ToLower().Contains("forest"))
        {
            isInitialized = false;
            InitializeSenlinScene();
        }
        else if (scene.name.ToLower().Contains("sample") || scene.name.ToLower().Contains("cabin") || scene.name.ToLower().Contains("hut"))
        {
            // Going to cabin scene - not our responsibility
            Debug.Log("[SenlinGameManager] Entered cabin scene, handing over control");
        }
    }

    void InitializeSenlinScene()
    {
        if (isInitialized) return;
        isInitialized = true;

        Debug.Log("[SenlinGameManager] Initializing senlin scene...");

        // Auto-create TiledObjectLoader if enabled
        if (autoCreateTiledObjectLoader && TiledObjectLoader.instance == null)
        {
            GameObject loaderObj = new GameObject("TiledObjectLoader");
            TiledObjectLoader loader = loaderObj.AddComponent<TiledObjectLoader>();
            loader.tmxFilePath = tmxFilePath;
            Debug.Log("[SenlinGameManager] Auto-created TiledObjectLoader");
        }

        // Auto-create SceneTransitionManager if not exists
        if (SceneTransitionManager.instance == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            managerObj.AddComponent<SceneTransitionManager>();
            Debug.Log("[SenlinGameManager] Auto-created SceneTransitionManager");
        }

        // Setup camera follow
        SetupCameraFollow();

        // Handle spawn position
        if (hasSpawnPosition)
        {
            ApplySpawnPosition();
        }

        Debug.Log("[SenlinGameManager] Senlin scene initialization complete");
    }

    void SetupCameraFollow()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[SenlinGameManager] Main Camera not found");
            return;
        }

        CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
        if (camFollow == null)
        {
            camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
            Debug.Log("[SenlinGameManager] Added CameraFollow to Main Camera");
        }

        // Set target to player
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            camFollow.target = player.transform;
            Debug.Log("[SenlinGameManager] CameraFollow target set to Player");
        }
        else
        {
            Debug.LogWarning("[SenlinGameManager] Player not found for CameraFollow");
        }
    }

    void ApplySpawnPosition()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = playerSpawnPosition;
            Debug.Log($"[SenlinGameManager] Player positioned at: {playerSpawnPosition}");
        }
        hasSpawnPosition = false;
    }

    /// <summary>
    /// Set the spawn position for the next scene load
    /// </summary>
    public void SetSpawnPosition(Vector3 position)
    {
        playerSpawnPosition = position;
        hasSpawnPosition = true;
    }

    /// <summary>
    /// Transition to the cabin scene
    /// </summary>
    public void GoToCabin(Vector3 spawnPos = default)
    {
        if (spawnPos != default)
        {
            SetSpawnPosition(spawnPos);
        }

        Debug.Log("[SenlinGameManager] Transitioning to cabin scene");
        if (SceneTransitionManager.instance != null)
        {
            SceneTransitionManager.instance.TransitionToScene(cabinSceneName, playerSpawnPosition);
        }
        else
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            SceneTransitionManager manager = managerObj.AddComponent<SceneTransitionManager>();
            manager.TransitionToScene(cabinSceneName, playerSpawnPosition);
        }
    }

    /// <summary>
    /// Reload the senlin scene
    /// </summary>
    public void ReloadSenlin()
    {
        isInitialized = false;
        SceneManager.LoadScene(currentSceneName);
    }
}