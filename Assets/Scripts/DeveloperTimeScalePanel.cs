using UnityEngine;

public sealed class DeveloperTimeScalePanel : MonoBehaviour
{
    private const float MinTimeScale = 0.25f;
    private const float MaxTimeScale = 8f;

    [SerializeField] private bool showPanel = true;
    [SerializeField] private float baselineRouteMinutes = 29.6f;
    [SerializeField, Range(MinTimeScale, MaxTimeScale)] private float timeScale = 3f;
    [SerializeField, Range(1f, 2f)] private float uiScale = 1.5f;

    private Rect windowRect = new(16f, 16f, 280f, 125f);

    private void Awake()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        SetTimeScale(timeScale);
#else
        enabled = false;
#endif
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (showPanel)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "Developer Test Speed");
            GUI.matrix = previousMatrix;
        }
#endif
    }

    private void DrawWindow(int windowId)
    {
        GUILayout.Label($"Simulation: {timeScale:0.00}x");
        SetTimeScale(GUILayout.HorizontalSlider(timeScale, MinTimeScale, MaxTimeScale));
        GUILayout.Label($"Estimated route: {baselineRouteMinutes / timeScale:0.0} min");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Normal 1x"))
            SetTimeScale(1f);
        if (GUILayout.Button("Hide"))
            showPanel = false;
        GUILayout.EndHorizontal();
        GUI.DragWindow();
    }

    public void SetTimeScale(float value)
    {
        timeScale = Mathf.Clamp(value, MinTimeScale, MaxTimeScale);
        Time.timeScale = timeScale;
    }

    private void OnDisable() => ResetTimeScale();
    private void OnDestroy() => ResetTimeScale();

    private static void ResetTimeScale() => Time.timeScale = 1f;
}
