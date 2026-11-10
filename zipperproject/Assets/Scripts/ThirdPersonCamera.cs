using UnityEngine;

/// <summary>
/// 第三人称环绕相机：鼠标转视角、滚轮缩放、跟随平滑、球体探测避免穿墙。
/// </summary>
[DisallowMultipleComponent]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("目标")]
    [SerializeField] Transform target;
    [SerializeField] Vector3 pivotOffset = new Vector3(0f, 0.85f, 0f);

    [Header("距离")]
    [SerializeField] float distance = 5.2f;
    [SerializeField] float minDistance = 1.8f;
    [SerializeField] float maxDistance = 9f;
    [SerializeField] float zoomSpeed = 0.7f;

    [Header("鼠标")]
    [SerializeField] float mouseSensitivity = 2.4f;
    [SerializeField] float pitchMin = -25f;
    [SerializeField] float pitchMax = 72f;
    [SerializeField] bool lockCursor = true;

    [Header("平滑与碰撞")]
    [SerializeField] float pivotSmoothTime = 0.05f;
    [SerializeField] float collisionRadius = 0.28f;
    [SerializeField] float minCameraDistance = 0.35f;
    [SerializeField] LayerMask collisionMask = ~0;

    float yaw;
    float pitch = 14f;
    Vector3 pivot;
    Vector3 pivotVelocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null) pivot = target.position + pivotOffset;
    }

    /// <summary>一次性配置跟随参数（供场景构建器在编辑期调用）。</summary>
    public void Configure(Transform newTarget, float newDistance, Vector3 newPivotOffset, float newPitchMin, float newPitchMax, float newPitch = 14f)
    {
        target = newTarget;
        distance = newDistance;
        pivotOffset = newPivotOffset;
        pitchMin = newPitchMin;
        pitchMax = newPitchMax;
        pitch = Mathf.Clamp(newPitch, pitchMin, pitchMax);
        if (target != null) pivot = target.position + pivotOffset;
    }

    public void SnapBehindTarget()
    {
        if (target == null) return;
        pivot = target.position + pivotOffset;
        Vector3 back = Quaternion.Euler(pitch, yaw, 0f) * Vector3.back;
        transform.position = pivot + back * distance;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void Awake()
    {
        if (target != null) pivot = target.position + pivotOffset;
        if (lockCursor) SetCursorLocked(true);
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (lockCursor)
        {
            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                SetCursorLocked(true);
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
        }

        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        Vector3 wantedPivot = target.position + pivotOffset;
        pivot = Vector3.SmoothDamp(pivot, wantedPivot, ref pivotVelocity, pivotSmoothTime);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 back = rotation * Vector3.back;

        float allowed = distance;
        RaycastHit[] hits = Physics.SphereCastAll(pivot, collisionRadius, back, distance, collisionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null) continue;
            if (hitCollider.transform.IsChildOf(target)) continue;
            if (hits[i].distance < allowed) allowed = hits[i].distance;
        }

        float finalDistance = Mathf.Max(minCameraDistance, allowed - 0.05f);
        transform.position = pivot + back * finalDistance;
        transform.rotation = rotation;
    }

    static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void OnDisable()
    {
        if (!lockCursor) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
