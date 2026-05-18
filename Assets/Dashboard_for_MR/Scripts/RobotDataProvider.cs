using UnityEngine;

public interface IRobotDataProvider
{
    Vector3 GetRobotPosition();
    float GetRobotVelocity();
    float GetBatteryPercent();
    float GetPowerUsage();
    string GetState();
    string GetActiveTask();
    float GetTaskProgressPercent();
}

public class RobotDataProviderMock : MonoBehaviour, IRobotDataProvider
{
    // Simple mock for testing panels
    public Vector3 position = new Vector3(1.5f, 0f, 2.3f);
    public float velocity = 1.2f;
    public float battery = 78f;
    public float power = 1.1f;
    public string state = "Moving";
    public string task = "Inspect area";
    public float progress = 65f;

    public Vector3 GetRobotPosition() => position;
    public float GetRobotVelocity() => velocity;
    public float GetBatteryPercent() => battery;
    public float GetPowerUsage() => power;
    public string GetState() => state;
    public string GetActiveTask() => task;
    public float GetTaskProgressPercent() => progress;
}
