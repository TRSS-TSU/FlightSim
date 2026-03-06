using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowAircraftCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform aircraft;

    [Header("ND Orientation")]
    [Tooltip("If true: map rotates with aircraft heading (yaw only). If false: North-Up.")]
    [SerializeField]
    private bool headingUp = false;

    [Header("Camera Offsets (meters)")]
    [Tooltip(
        "X/Z are planar offsets. Y is computed from ND range; this value is used until the first range event."
    )]
    [SerializeField]
    private Vector3 offset = new Vector3(0f, 32078f, 0f);

    [Header("ND Range Source")]
    public NDRangeState rangeState;

    [Header("Tuning")]
    public float rebuildDelay = 0.1f;
    public float fovDeg = 60f;
    public float farClipMin = 80000f;

    private Coroutine pending;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fovDeg;
        cam.farClipPlane = farClipMin;
    }

    void OnEnable()
    {
        if (rangeState != null)
            rangeState.OnRangeChanged += HandleRangeChanged;

        // Ensure we apply once on enable even if no event fired yet.
        int nm = (rangeState != null) ? rangeState.CurrentRangeNm : 20;
        HandleRangeChanged(nm);
    }

    void OnDisable()
    {
        if (rangeState != null)
            rangeState.OnRangeChanged -= HandleRangeChanged;
        if (pending != null)
            StopCoroutine(pending);
        pending = null;
    }

    void LateUpdate()
    {
        if (!aircraft)
            return;

        Vector3 p = aircraft.position;

        // Follow position: X/Z from aircraft, Y from our computed offset.
        transform.position = new Vector3(p.x + offset.x, offset.y, p.z + offset.z);

        // Rotation: straight down + (optional) yaw only. Never pitch/roll.
        float yaw = headingUp ? aircraft.eulerAngles.y : 0f;
        transform.rotation = Quaternion.Euler(90f, yaw, 0f);
    }

    private void HandleRangeChanged(int nm)
    {
        if (pending != null)
            StopCoroutine(pending);
        pending = StartCoroutine(ApplyAfterDelay(nm));
    }

    private IEnumerator ApplyAfterDelay(int nm)
    {
        yield return new WaitForSeconds(rebuildDelay);

        // ND range is radius in NM -> full width (meters) is 2 * nm * 1852
        float widthM = 2f * nm * 1852f;

        // For a square RT (aspect=1), camera height that yields desired width:
        // width = 2 * H * tan(FOV/2)  => H = width / (2*tan(FOV/2))
        float H = widthM / (2f * Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView));

        float checkW = 2f * H * Mathf.Tan(0.5f * Mathf.Deg2Rad * cam.fieldOfView);
        Debug.Log($"[ND] range={nm}NM  H={H:F0}m  width={checkW:F0}m (expected {widthM:F0}m)");

        offset = new Vector3(offset.x, H, offset.z);
        cam.farClipPlane = Mathf.Max(farClipMin, H + 1000f);

        pending = null;
    }
}
