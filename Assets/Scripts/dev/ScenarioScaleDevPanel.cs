using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioScaleDevPanel : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField]
    private FlightPlan flightPlan;

    [SerializeField]
    private LocalTileGrid tileGrid;

    [SerializeField]
    private FollowAircraftCamera ndCamera;

    [Header("Scale Buttons")]
    [SerializeField]
    private Button scale002Button;

    [SerializeField]
    private Button scale004Button;

    [SerializeField]
    private Button scale006Button;

    [SerializeField]
    private Button scale008Button;

    [Header("Display")]
    [SerializeField]
    private TMP_Text trainingScaleText;

    [Header("Completion Timer")]
    [SerializeField]
    private NavAutopilot navAutopilot;

    [SerializeField]
    private Rigidbody aircraftRb;

    [SerializeField]
    private TMP_Text scenarioTimerText;

    [SerializeField]
    private float minimumGroundSpeedMps = 1f;

    private void Awake()
    {
        RegisterButton(scale002Button, 0.02f);
        RegisterButton(scale004Button, 0.04f);
        RegisterButton(scale006Button, 0.06f);
        RegisterButton(scale008Button, 0.08f);

        float currentScale = flightPlan ? flightPlan.trainingWorldScale : 0.05f;
        RefreshLabel(currentScale);
    }

    private void Update()
    {
        RefreshTimeRemaining();
    }

    private void RegisterButton(Button button, float scale)
    {
        if (!button)
            return;

        button.onClick.AddListener(() => SetTrainingScale(scale));
    }

    private void RefreshTimeRemaining()
    {
        if (!scenarioTimerText || !flightPlan || !navAutopilot || !aircraftRb)
            return;

        if (flightPlan.waypoints == null || flightPlan.waypoints.Length == 0)
        {
            scenarioTimerText.text = "Remaining: --:--";
            return;
        }

        float groundSpeed = new Vector2(
            aircraftRb.linearVelocity.x,
            aircraftRb.linearVelocity.z
        ).magnitude;

        if (groundSpeed < minimumGroundSpeedMps)
        {
            scenarioTimerText.text = "Remaining: --:--";
            return;
        }

        float remainingDistance = navAutopilot.activeDistance;

        for (int i = navAutopilot.activeIndex; i < flightPlan.waypoints.Length - 1; i++)
        {
            Transform current = flightPlan.waypoints[i];
            Transform next = flightPlan.waypoints[i + 1];

            if (!current || !next)
                continue;

            remainingDistance += Vector3.Distance(current.position, next.position);
        }

        float remainingSeconds = remainingDistance / groundSpeed;

        int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60f);

        bool complete =
            navAutopilot.activeIndex >= flightPlan.waypoints.Length - 1
            && navAutopilot.activeDistance < navAutopilot.captureRadius;

        scenarioTimerText.text = complete
            ? "Complete: 00:00"
            : $"Remaining: {minutes:00}:{seconds:00}";
    }

    private void SetTrainingScale(float value)
    {
        if (flightPlan)
        {
            flightPlan.trainingWorldScale = value;
            flightPlan.RebuildCurrentRoute();
        }

        if (tileGrid)
        {
            tileGrid.trainingWorldScale = value;
            tileGrid.Rebuild();
        }

        if (ndCamera)
            ndCamera.trainingWorldScale = value;

        RefreshLabel(value);
    }

    private void RefreshLabel(float value)
    {
        if (trainingScaleText)
            trainingScaleText.text = $"Training Scale: {value:0.00}x";
    }
}
