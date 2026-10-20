using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    public float smoothSpeed = 2.5f;

    [Header("地图边界限制")]
    public float mapWidth = 480f;
    public float mapHeight = 320f;

    private float cameraHalfWidth;
    private float cameraHalfHeight;

    void Start()
    {
        float cameraSize = GetComponent<Camera>().orthographicSize;
        float aspectRatio = GetComponent<Camera>().aspect;
        cameraHalfWidth = cameraSize * aspectRatio / transform.localScale.x;
        cameraHalfHeight = cameraSize / transform.localScale.x;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: 没有设置跟随目标！");
            return;
        }

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);

        float minX = cameraHalfWidth - mapWidth / 2;
        float maxX = mapWidth / 2 - cameraHalfWidth;
        float minY = cameraHalfHeight - mapHeight / 2;
        float maxY = mapHeight / 2 - cameraHalfHeight;

        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
