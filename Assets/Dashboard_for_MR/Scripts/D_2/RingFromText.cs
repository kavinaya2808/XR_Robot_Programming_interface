using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RingDebugFill : MonoBehaviour
{
    [Header("Assign UI")]
    public Image ringFillImage;         // assign your RingFG Image (Image.Type = Filled)
    public TMP_Text percentTextTMP;     // assign if using TextMeshPro
    public Text percentTextLegacy;      // assign if using Unity UI Text

    [Header("Debug")]
    [Tooltip("If true, script will repeatedly read the text and apply to ring.")]
    public bool active = true;
    public float pollInterval = 0.2f;
    public bool smooth = true;
    public float smoothDuration = 0.4f;

    float _current = 0f;
    Coroutine _anim;

    void Start()
    {
        if (ringFillImage == null)
            Debug.LogError("[RingDebugFill] ringFillImage NOT assigned! Assign the Image in inspector.");
        else
            Debug.Log("[RingDebugFill] ringFillImage assigned. Initial fillAmount = " + ringFillImage.fillAmount);

        if (active) StartCoroutine(PollTextCoroutine());
    }

    IEnumerator PollTextCoroutine()
    {
        while (true)
        {
            float parsed = ParsePercent();
            string rawText = GetTextRaw();
            
            if (smooth)
                AnimateTo(parsed);
            else
                ApplyInstant(parsed);

            // Debug: Log every 5 polls (every 1 second at 5Hz)
            if (Time.frameCount % 5 == 0)
            {
                Debug.Log($"[RingDebugFill] Text='{rawText}' Parsed={parsed:F1}% FillAmount={ringFillImage?.fillAmount:F3}");
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, pollInterval));
        }
    }

    string GetTextRaw()
    {
        if (percentTextTMP != null) return percentTextTMP.text;
        if (percentTextLegacy != null) return percentTextLegacy.text;
        return "<no-text-assigned>";
    }

    float ParsePercent()
    {
        string s = GetTextRaw();
        if (string.IsNullOrWhiteSpace(s)) return _current;

        s = s.Trim();
        if (s.EndsWith("%")) s = s.Substring(0, s.Length - 1).Trim();

        // Try invariant parse first (dot decimal)
        if (float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
            return Mathf.Clamp(f, 0f, 100f);

        // Then try local culture (comma decimals)
        if (float.TryParse(s, out f))
            return Mathf.Clamp(f, 0f, 100f);

        // last fallback: remove non-digits except dot/comma
        var cleaned = System.Text.RegularExpressions.Regex.Replace(s, @"[^\d\.,\-]", "");
        if (float.TryParse(cleaned, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f))
            return Mathf.Clamp(f, 0f, 100f);

        return _current;
    }

    void ApplyInstant(float percent)
    {
        _current = Mathf.Clamp(percent, 0f, 100f);
        if (ringFillImage != null)
            ringFillImage.fillAmount = _current / 100f;
    }

    void AnimateTo(float target)
    {
        target = Mathf.Clamp(target, 0f, 100f);
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(Animate(_current, target, Mathf.Max(0.0001f, smoothDuration)));
    }

    IEnumerator Animate(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(from, to, Mathf.SmoothStep(0f,1f,t/duration));
            if (ringFillImage != null) ringFillImage.fillAmount = v / 100f;
            _current = v;
            yield return null;
        }
        ApplyInstant(to);
        _anim = null;
    }

    // Editor helper to test fill: press the button in Inspector (works if you add a small custom editor),
    // but easier: drag the Fill Amount slider in the Image component while game is running.
}
