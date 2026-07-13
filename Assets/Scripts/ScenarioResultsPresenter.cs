using UnityEngine;

/// <summary>Display-only results scene presenter. Metrics are finalized by FlightSession.</summary>
public sealed class ScenarioResultsPresenter : MonoBehaviour
{
    private void OnGUI()
    {
        var session = FlightSession.Instance;
        if (!session)
            return;

        var record = session.Record;
        var scale = Mathf.Clamp(Screen.height / 1080f, 1f, 1.6f);
        var titleStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = Mathf.RoundToInt(28f * scale)
        };
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(24f * scale)
        };
        var buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(26f * scale)
        };
        var area = new Rect(Screen.width * 0.1f, Screen.height * 0.08f, Screen.width * 0.8f, Screen.height * 0.84f);
        var padding = 30f * scale;
        GUI.Box(area, "SCENARIO RESULTS", titleStyle);
        GUILayout.BeginArea(new Rect(area.x + padding, area.y + 56f * scale, area.width - padding * 2f, area.height - 86f * scale));
        GUILayout.Label(record.scenarioName, labelStyle);
        GUILayout.Label($"Status: {record.status}", labelStyle);
        GUILayout.Label($"Elapsed: {FormatTime(record.elapsedSeconds)}", labelStyle);
        GUILayout.Label($"Waypoints completed: {record.waypointsCompleted}", labelStyle);
        GUILayout.Label($"Route modifications: {record.routeModifications}", labelStyle);
        GUILayout.Label($"Skipped required checks: {record.skippedRequiredChecks}", labelStyle);
        GUILayout.Label($"Hold circuits completed: {record.holdCircuitsCompleted}", labelStyle);
        GUILayout.Label($"Touchdown reached: {record.touchdownReached}", labelStyle);
        GUILayout.Label($"Final stop reached: {record.finalStopReached}", labelStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back to Menu", buttonStyle, GUILayout.Height(80f * scale)))
            session.ReturnToMenu();
        GUILayout.Space(12f * scale);
        if (GUILayout.Button("End", buttonStyle, GUILayout.Height(80f * scale)))
            session.Quit();
        GUILayout.EndArea();
    }

    private static string FormatTime(float seconds)
    {
        var time = System.TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}
