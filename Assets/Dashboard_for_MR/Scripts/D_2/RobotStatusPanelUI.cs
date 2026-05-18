using UnityEngine;
using TMPro;

public class RobotStatusPanelUI : MonoBehaviour
{
    public string robotId = "Robot_1";

    public TextMeshProUGUI robotIdText;
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI activeTaskText;

    public float updateHz = 5f;
    float nextTime = 0f;

    void Update()
    {
        if (Time.time < nextTime) return;
        nextTime = Time.time + 1f / updateHz;
        Refresh();
    }

    public void Refresh()
    {
        if (!RobotTelemetry.Registry.TryGetValue(robotId, out var t))
        {
            robotIdText.text = "No robot";
            stateText.text = "-";
            activeTaskText.text = "-";
            return;
        }

        robotIdText.text = t.robotId;
        stateText.text = t.currentState;
        activeTaskText.text = t.activeTask;
    }
}
