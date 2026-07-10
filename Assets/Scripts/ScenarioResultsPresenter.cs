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
        var area = new Rect(Screen.width * 0.1f, Screen.height * 0.08f, Screen.width * 0.8f, Screen.height * 0.84f);
        GUI.Box(area, "SCENARIO RESULTS");
        GUILayout.BeginArea(new Rect(area.x + 30f, area.y + 50f, area.width - 60f, area.height - 80f));
        GUILayout.Label(record.scenarioName);
        GUILayout.Label($"Status: {record.status}");
        GUILayout.Label($"Elapsed: {FormatTime(record.elapsedSeconds)}");
        GUILayout.Label($"Waypoints completed: {record.waypointsCompleted}");
        GUILayout.Label($"Route modifications: {record.routeModifications}");
        GUILayout.Label($"Skipped required checks: {record.skippedRequiredChecks}");
        GUILayout.Label($"Hold circuits completed: {record.holdCircuitsCompleted}");
        GUILayout.Label($"Touchdown reached: {record.touchdownReached}");
        GUILayout.Label($"Final stop reached: {record.finalStopReached}");
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Back to Menu", GUILayout.Height(48f)))
            session.ReturnToMenu();
        if (GUILayout.Button("End", GUILayout.Height(48f)))
            session.Quit();
        GUILayout.EndArea();
    }

    private static string FormatTime(float seconds)
    {
        var time = System.TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
    }
}
