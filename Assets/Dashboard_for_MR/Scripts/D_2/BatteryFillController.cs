using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class BatteryFillController : MonoBehaviour
{
    [Header("UI")]
    public Image fillImage;              // Image set to Filled (Horizontal)
    public TextMeshProUGUI percentText;  // optional

    [Header("Colors")]
    public Color lowColor = Color.red;
    public Color midColor = new Color(1f, 0.8f, 0f); // amber
    public Color highColor = Color.green;

    [Header("Animation")]
    public bool animateCharging = true;
    public float chargingPulseSpeed = 3f;

    void Awake()
    {
        if (fillImage == null)
            Debug.LogWarning("fillImage not assigned on " + name);
    }

    // call this from your battery system: 0..1
    public void SetBatteryNormalized(float normalized, bool isCharging = false)
    {
        normalized = Mathf.Clamp01(normalized);
        fillImage.fillAmount = normalized;

        // optional percent text
        if (percentText) percentText.text = Mathf.RoundToInt(normalized * 100f) + "%";

        // color gradient (low→mid→high)
        if (normalized < 0.25f) fillImage.color = lowColor;
        else if (normalized < 0.7f) fillImage.color = midColor;
        else fillImage.color = highColor;

        // charging pulse coroutine
        StopAllCoroutines();
        if (isCharging && animateCharging)
            StartCoroutine(ChargingPulse());
    }

    IEnumerator ChargingPulse()
    {
        float baseAlpha = fillImage.color.a;
        while (true)
        {
            float t = (Mathf.Sin(Time.time * chargingPulseSpeed) + 1f) * 0.5f; // 0..1
            Color c = fillImage.color;
            c.a = Mathf.Lerp(baseAlpha * 0.5f, baseAlpha, t);
            fillImage.color = c;
            yield return null;
        }
    }
}
