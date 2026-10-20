using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Unified interface for interactable objects
/// </summary>
public interface IInteractable
{
    void Interact();
    string GetPrompt();
    void SetHighlight(bool highlight);
}

/// <summary>
/// Interactable Item Highlight System - Detects items near the player and highlights them
/// Persists across scenes, auto-finds the player in each new scene
/// </summary>
public class InteractableHighlighter : MonoBehaviour
{
    public static InteractableHighlighter instance;

    [Header("Detection Settings")]
    public float detectionRadius = 2f;
    public LayerMask interactableLayer;
    public int maxDetections = 10;

    [Header("Highlight Settings")]
    public Color proximityHighlightColor = new Color(1f, 0.95f, 0.6f, 1f);
    public float highlightLerpSpeed = 10f;

    [Header("Input Settings")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode mouseModeKey = KeyCode.LeftAlt;

    private bool isMouseMode = false;

    private List<IInteractable> nearbyInteractables = new List<IInteractable>();
    private IInteractable closestInteractable;
    private List<MonoBehaviour> registeredObjects = new List<MonoBehaviour>();

    private Transform playerTransform;

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[InteractableHighlighter] Scene loaded: {scene.name}");
        isMouseMode = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        FindPlayer();
        RefreshRegisteredObjects();
    }

    void Start()
    {
        if (interactableLayer.value == 0)
        {
            interactableLayer = ~0;
            Debug.Log("[InteractableHighlighter] interactableLayer not set, auto-detecting all layers");
        }

        FindPlayer();
        RefreshRegisteredObjects();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) playerObj = pc.gameObject;
        }

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            transform.position = playerTransform.position;
            Debug.Log($"[InteractableHighlighter] Found player at: {playerTransform.position}");
        }
        else
        {
            Debug.LogWarning("[InteractableHighlighter] Player not found in scene!");
        }
    }

    void RefreshRegisteredObjects()
    {
        registeredObjects.Clear();

        var existing = FindObjectsOfType<TiledObjectInteractable>();
        foreach (var obj in existing)
        {
            if (!registeredObjects.Contains(obj))
                registeredObjects.Add(obj);
        }

        var tiled = FindObjectsOfType<TiledInteractableObject>();
        foreach (var obj in tiled)
        {
            if (!registeredObjects.Contains(obj))
                registeredObjects.Add(obj);
        }

        var classic = FindObjectsOfType<InteractableObject>();
        foreach (var obj in classic)
        {
            if (!registeredObjects.Contains(obj))
                registeredObjects.Add(obj);
        }

        Debug.Log($"[InteractableHighlighter] Registered {registeredObjects.Count} interactable objects");
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        transform.position = playerTransform.position;

        CheckMouseModeToggle();
        DetectNearbyInteractables();
        UpdateHighlights();
        CheckInteractionInput();
    }

    void CheckMouseModeToggle()
    {
        bool wasMouseMode = isMouseMode;
        isMouseMode = Input.GetKey(mouseModeKey);

        if (isMouseMode != wasMouseMode)
        {
            Debug.Log($"Mouse interaction mode: {(isMouseMode ? "On" : "Off")}");
            Cursor.visible = isMouseMode;

            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                if (isMouseMode)
                    pc.DisableMovement();
                else
                    pc.EnableMovement();
            }
        }
    }

    void DetectNearbyInteractables()
    {
        nearbyInteractables.Clear();
        closestInteractable = null;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            transform.position, detectionRadius, interactableLayer);

        float closestDistance = float.MaxValue;

        foreach (Collider2D col in colliders)
        {
            IInteractable interactable = GetInteractableComponent(col);

            if (interactable != null)
            {
                float distance = Vector2.Distance(transform.position, col.transform.position);
                nearbyInteractables.Add(interactable);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        // Check registered objects
        foreach (MonoBehaviour obj in registeredObjects)
        {
            if (obj == null || !obj.gameObject.activeSelf) continue;

            IInteractable interactable = obj as IInteractable;
            if (interactable == null) continue;

            float distance = Vector2.Distance(transform.position, obj.transform.position);
            if (distance <= detectionRadius && !nearbyInteractables.Contains(interactable))
            {
                nearbyInteractables.Add(interactable);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        UpdateInteractPrompt();
    }

    IInteractable GetInteractableComponent(Collider2D col)
    {
        TiledObjectInteractable tiledObjInteractable = col.GetComponent<TiledObjectInteractable>();
        if (tiledObjInteractable != null) return tiledObjInteractable;

        TiledInteractableObject tiledInteractable = col.GetComponent<TiledInteractableObject>();
        if (tiledInteractable != null) return tiledInteractable;

        InteractableObject interactable = col.GetComponent<InteractableObject>();
        if (interactable != null) return interactable;

        return null;
    }

    void UpdateHighlights()
    {
        foreach (IInteractable interactable in nearbyInteractables)
        {
            if (interactable == null) continue;

            bool isClosest = interactable == closestInteractable;
            interactable.SetHighlight(isClosest);
        }
    }

    void UpdateInteractPrompt()
    {
        if (UIManager.instance == null && SenlinUIManager.instance == null) return;

        string prompt = closestInteractable != null ? closestInteractable.GetPrompt() : null;

        if (UIManager.instance != null)
        {
            if (prompt != null) UIManager.instance.ShowInteractPrompt(prompt);
            else UIManager.instance.HideInteractPrompt();
        }

        if (SenlinUIManager.instance != null)
        {
            if (prompt != null) SenlinUIManager.instance.ShowInteractPrompt(prompt);
            else SenlinUIManager.instance.HideInteractPrompt();
        }
    }

    void CheckInteractionInput()
    {
        if (DialogSystem.instance != null && DialogSystem.instance.IsInDialog())
            return;

        if (Input.GetKeyDown(interactKey))
        {
            if (closestInteractable != null)
            {
                closestInteractable.Interact();
            }
        }

        if (isMouseMode && Input.GetMouseButtonDown(0))
        {
            TryMouseInteract();
        }
    }

    void TryMouseInteract()
    {
        if (Camera.main == null) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos, interactableLayer);

        if (hit != null)
        {
            IInteractable interactable = GetInteractableComponent(hit);
            if (interactable != null)
            {
                float dist = Vector2.Distance(transform.position, hit.transform.position);
                if (dist <= detectionRadius)
                {
                    interactable.Interact();
                }
                else
                {
                    DialogSystem.instance?.StartDialog("Too far, move closer.");
                }
            }
        }
    }

    public void RegisterInteractable(MonoBehaviour interactable)
    {
        if (interactable != null && !registeredObjects.Contains(interactable))
        {
            registeredObjects.Add(interactable);
        }
    }

    public void UnregisterInteractable(MonoBehaviour interactable)
    {
        registeredObjects.Remove(interactable);

        if (closestInteractable != null && closestInteractable == interactable as IInteractable)
        {
            closestInteractable = null;
        }
    }

    public IInteractable GetClosestInteractable()
    {
        return closestInteractable;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}