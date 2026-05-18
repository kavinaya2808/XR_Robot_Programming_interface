using UnityEngine;
using TMPro;

public class PanelController1 : MonoBehaviour
{
    public PanelType panelType;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText; // simple single-field for example
    public GameObject minimizeButton;
    public GameObject closeButton;

    // Hook this to your robot telemetry provider
    private IRobotDataProvider dataProvider;

    public void Initialize(PanelType type)
    {
        panelType = type;
        if (titleText) titleText.text = type.ToString();
        // Connect to data provider (you need to implement how to find/assign the provider)
        dataProvider = FindObjectOfType<RobotDataProviderMock>(); // example
        UpdateContent();
    }

    void Update()
    {
        // update live content — consider using events instead of polling for efficiency
        UpdateContent();
    }

    void UpdateContent()
    {
        if (dataProvider == null) return;

        switch (panelType)
        {
            case PanelType.Position:
                var p = dataProvider.GetRobotPosition();
                bodyText.text = $"X: {p.x:F2}\nY: {p.y:F2}\nZ: {p.z:F2}\nVel: {dataProvider.GetRobotVelocity():F2} m/s";
                break;
            case PanelType.Battery:
                bodyText.text = $"Battery: {dataProvider.GetBatteryPercent():F0}%\nPower: {dataProvider.GetPowerUsage():F2}kW";
                break;
            case PanelType.RobotStatus:
                bodyText.text = $"State: {dataProvider.GetState()}\nTask: {dataProvider.GetActiveTask()}";
                break;
            case PanelType.TaskProgress:
                bodyText.text = $"Task: {dataProvider.GetActiveTask()}\nProgress: {dataProvider.GetTaskProgressPercent():F0}%";
                break;
        }
    }

    public void OnMinimize()
    {
        // hide content while keeping header
        // implement UI transitions as desired
        transform.localScale = new Vector3(1, 0.2f, 1);
    }

    public void OnClose()
    {
        Destroy(gameObject);
    }
}
