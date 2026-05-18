// BatteryPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BatteryPanelUI : MonoBehaviour
{
    [Header("Robot")]
    public string robotId = "Robot_1";

    [Header("Text fields")]
    public TextMeshProUGUI chargePercentText;
    public TextMeshProUGUI estimatedTimeText;
    public TextMeshProUGUI powerConsumptionText;

    [Header("Update")]
    public float updateHz = 2f;
    float nextTime = 0f;

    [Header("Battery images (assign in inspector)")]
    public GameObject BatteryFull;        // 75 - 100%
    public GameObject BatteryQuadFill;    // 50 - 75%
    public GameObject BatteryHalfFill;    // 25 - 50%
    public GameObject BatteryQuarterFill; // 10 - 25%
    public GameObject BatteryLess;        // 1 - 10%
    public GameObject BatteryEmpty;       // 0%

    [Header("Tube / Progress (Estimated-time visualization)")]
    public Image tubeFillImage;
    public RectTransform tubeBackgroundRect;
    public bool useTimeFractionForWidth = false;
    public float maxEstimatedSeconds = 3600f;
    public float backgroundInitialWidth = 0f;
    public float backgroundMinWidth = 10f;

    [Header("Power graph (sparkline)")]
    public SparklineUI powerSparkline;
    public int sparklineSampleCount = 60;
    public float sparklineSamplesPerSecond = 1f;

    float nextSparkSampleTime = 0f;

    [Header("Color thresholds")]
    public bool colorThresholds = true;
    public Color colorHigh = new Color(0.2f, 0.8f, 0.2f);
    public Color colorMedium = new Color(1f, 0.8f, 0.0f);
    public Color colorLow = new Color(1f, 0.4f, 0.0f);
    public Color colorCritical = new Color(1f, 0.15f, 0.15f);

    [Header("Animation smoothing")]
    public float fillSmoothSpeed = 6f;
    public float widthSmoothSpeed = 8f;

    private float displayedFill = 0f;
    private float targetFill = 0f;
    private float displayedWidth = 0f;
    private float targetWidth = 0f;
    private float tubeHeight = 0f;

    void Start()
    {
        // tube init
        if (tubeFillImage != null)
        {
            displayedFill = tubeFillImage.fillAmount;
            targetFill = displayedFill;
        }

        if (tubeBackgroundRect != null)
        {
            tubeHeight = tubeBackgroundRect.sizeDelta.y;
            if (backgroundInitialWidth <= 0f)
                backgroundInitialWidth = tubeBackgroundRect.sizeDelta.x;
            backgroundInitialWidth = Mathf.Max(backgroundInitialWidth, backgroundMinWidth);
            displayedWidth = tubeBackgroundRect.sizeDelta.x;
            targetWidth = displayedWidth;
        }

        // sparkline init
        if (powerSparkline != null)
        {
            powerSparkline.sampleCount = Mathf.Max(2, sparklineSampleCount);
            // style to match your neon theme
            powerSparkline.lineColor = new Color(0.1294f, 0.3255f, 0.6431f, 1f);
            powerSparkline.fillColor = new Color(0.1294f, 0.3255f, 0.6431f, 1f);
            powerSparkline.ClearSamples();
        }
        nextSparkSampleTime = Time.time;
    }

    void Update()
    {
        // timed refresh for UI text and tube
        if (Time.time >= nextTime)
        {
            nextTime = Time.time + 1f / Mathf.Max(0.1f, updateHz);
            Refresh();
        }

        // animate tube fill smoothly
        if (tubeFillImage != null)
        {
            displayedFill = Mathf.Lerp(displayedFill, targetFill, Time.deltaTime * fillSmoothSpeed);
            displayedFill = Mathf.Clamp01(displayedFill);
            tubeFillImage.fillAmount = displayedFill;
        }

        if (tubeBackgroundRect != null)
        {
            displayedWidth = Mathf.Lerp(displayedWidth, targetWidth, Time.deltaTime * widthSmoothSpeed);
            displayedWidth = Mathf.Clamp(displayedWidth, backgroundMinWidth, backgroundInitialWidth);
            tubeBackgroundRect.sizeDelta = new Vector2(displayedWidth, tubeHeight);
        }

        // sample power for sparkline at configured rate
        if (powerSparkline != null && Time.time >= nextSparkSampleTime)
        {
            nextSparkSampleTime = Time.time + 1f / Mathf.Max(0.01f, sparklineSamplesPerSecond);

            if (RobotTelemetry.Registry.TryGetValue(robotId, out var t))
            {
                float powerW = t.idlePowerW + t.dynamicPowerPerSpeed * t.GetSpeed();
                powerSparkline.AddSample(powerW);
            }
            else
            {
                powerSparkline.AddSample(0f);
            }
        }
    }

    public void Refresh()
    {
        if (!RobotTelemetry.Registry.TryGetValue(robotId, out var t))
        {
            chargePercentText.text = "No robot";
            estimatedTimeText.text = "-";
            powerConsumptionText.text = "-";
            ShowOnly(null);
            targetFill = 0f;
            targetWidth = backgroundMinWidth;
            return;
        }

        float percent = Mathf.Clamp(t.batteryPercent, 0f, 100f);
        chargePercentText.text = $"{percent:F0}%";

        float seconds = t.EstimatedBatterySecondsRemaining();
        estimatedTimeText.text = float.IsInfinity(seconds) ? "∞" : FormatSeconds(seconds);

        float powerW = t.idlePowerW + t.dynamicPowerPerSpeed * t.GetSpeed();
        powerConsumptionText.text = $"{powerW:F2} W";

        UpdateBatteryVisual(percent);

        targetFill = Mathf.Clamp01(percent / 100f);

        float fractionForWidth = targetFill;
        if (useTimeFractionForWidth && maxEstimatedSeconds > 0f && !float.IsInfinity(seconds) && seconds >= 0f)
            fractionForWidth = Mathf.Clamp01(seconds / maxEstimatedSeconds);

        targetWidth = Mathf.Lerp(backgroundMinWidth, backgroundInitialWidth, fractionForWidth);

        if (tubeFillImage != null && colorThresholds)
        {
            if (percent > 75f) tubeFillImage.color = colorHigh;
            else if (percent > 50f) tubeFillImage.color = colorMedium;
            else if (percent > 25f) tubeFillImage.color = colorLow;
            else tubeFillImage.color = colorCritical;
        }
    }

    void UpdateBatteryVisual(float percent)
    {
        if (percent <= 0f) ShowOnly(BatteryEmpty);
        else if (percent <= 10f) ShowOnly(BatteryLess);
        else if (percent <= 25f) ShowOnly(BatteryQuarterFill);
        else if (percent <= 50f) ShowOnly(BatteryHalfFill);
        else if (percent <= 75f) ShowOnly(BatteryQuadFill);
        else ShowOnly(BatteryFull);
    }

    void ShowOnly(GameObject active)
    {
        SetActiveSafe(BatteryFull, active == BatteryFull);
        SetActiveSafe(BatteryQuadFill, active == BatteryQuadFill);
        SetActiveSafe(BatteryHalfFill, active == BatteryHalfFill);
        SetActiveSafe(BatteryQuarterFill, active == BatteryQuarterFill);
        SetActiveSafe(BatteryLess, active == BatteryLess);
        SetActiveSafe(BatteryEmpty, active == BatteryEmpty);
    }

    void SetActiveSafe(GameObject go, bool value)
    {
        if (go == null) return;
        if (go.activeSelf != value) go.SetActive(value);
    }

    string FormatSeconds(float sec)
    {
        TimeSpan ts = TimeSpan.FromSeconds(Mathf.Max(0f, sec));
        if (ts.TotalHours >= 1.0) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1.0) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    // External API to set tube percent if needed
    public void SetTubePercent(float percent)
    {
        targetFill = Mathf.Clamp01(percent / 100f);
        targetWidth = Mathf.Lerp(backgroundMinWidth, backgroundInitialWidth, targetFill);
    }
}
