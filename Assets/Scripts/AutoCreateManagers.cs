using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoCreateManagers : MonoBehaviour
{
    public string tmxFilePath = "Sprites/森林.tmx";

    void Awake()
    {
        CreateSceneTransitionManager();
    }

    void Start()
    {
        CreateTiledObjectLoader();
    }

    void CreateSceneTransitionManager()
    {
        if (SceneTransitionManager.instance == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            managerObj.AddComponent<SceneTransitionManager>();
            Debug.Log("自动创建 SceneTransitionManager");
        }
    }

    void CreateTiledObjectLoader()
    {
        if (TiledObjectLoader.instance == null)
        {
            GameObject loaderObj = new GameObject("TiledObjectLoader");
            TiledObjectLoader loader = loaderObj.AddComponent<TiledObjectLoader>();
            loader.tmxFilePath = tmxFilePath;
            Debug.Log("自动创建 TiledObjectLoader");
        }
    }
}