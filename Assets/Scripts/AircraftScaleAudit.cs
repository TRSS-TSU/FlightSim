using UnityEngine;

public class AircraftScaleAudit : MonoBehaviour
{
    public Transform aircraftRoot;
    public Transform aircraftVisual;

    [Header("Reference")]
    public float desiredAircraftLengthM = 14f;

    [Header("Optional Runway Points")]
    public Transform startPoint;
    public Transform thresholdPoint;

    [ContextMenu("Audit Aircraft Scale")]
    public void Audit()
    {
        if (!aircraftVisual)
        {
            Debug.LogWarning("[AircraftScaleAudit] aircraftVisual is not assigned.");
            return;
        }

        Renderer[] renderers = aircraftVisual.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("[AircraftScaleAudit] No renderers found under aircraftVisual.");
            return;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        Vector3 size = b.size;

        float longestAxis = Mathf.Max(size.x, size.y, size.z);
        float suggestedScaleFactor =
            longestAxis > 0.001f ? desiredAircraftLengthM / longestAxis : 1f;

        _ = suggestedScaleFactor;
    }
}
