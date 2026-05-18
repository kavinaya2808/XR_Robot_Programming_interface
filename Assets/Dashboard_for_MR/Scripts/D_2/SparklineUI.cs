// SparklineUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sparkline UI: draws a stock-market style polyline (time on X, value on Y),
/// optional filled area, grid, and explicit X/Y axes. Works with world-space or screen-space canvases.
/// Auto-scales line thickness and dot radius based on rect height when enabled.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
[ExecuteAlways]
public class SparklineUI : Graphic
{
    [Header("Buffer")]
    [Tooltip("Number of samples stored horizontally")]
    public int sampleCount = 120;

    [Tooltip("Moving average window size for smoothing (1 = no smoothing)")]
    [Range(1, 11)]
    public int smoothingWindow = 3;

    [Header("Sampling / Scaling")]
    [Tooltip("If true, autoscale Y range to data; otherwise use fixedMinY..fixedMaxY")]
    public bool autoScaleY = false;
    public float fixedMinY = 0f;
    public float fixedMaxY = 120f;
    [Range(0f, 0.5f)]
    public float yPaddingPercent = 0.05f;

    [Header("Appearance")]
    public Color lineColor = new Color(0.8196f, 0f, 0f, 1f); // neon pink
    [Tooltip("Base line width in UI units (pixels or world units) if autoScaleThickness=false")]
    public float lineWidth = 2f;
    [Tooltip("Draw filled area under the line")]
    public bool drawFilled = false;
    public Color fillColor = new Color(0.8196f, 0f, 0f, 1f); // subtle pink fill

    [Header("Axes & Grid")]
    public bool drawAxes = true;
    public Color axisColor = new Color(1f,1f,1f,0.6f);
    [Range(0.001f, 0.5f)] public float axisWidthScale = 0.02f; // fraction of rect.height
    public bool drawGrid = true;
    public int gridHorizontalLines = 3;
    public Color gridColor = new Color(1f,1f,1f,0.06f);
    [Header("Axes")]
    public float axisPixelThickness = 3f;  // SAME thickness for X & Y


    [Header("Time ticks")]
    public bool drawTimeTicks = true; // left/mid/right ticks on x-axis
    public Color tickColor = new Color(1f,1f,1f,0.18f);

    [Header("Latest marker")]
    public bool drawLatestDot = true;
    public Color latestDotColor = Color.white;
    public float latestDotRadius = 4f; // used when autoScaleThickness=false

    [Header("Auto-thickness (for world-space)")]
    [Tooltip("If true, automatically scale line width and dot radius based on rect.height")]
    public bool autoScaleThickness = true;
    [Tooltip("line width = rect.height * lineWidthScale")]
    public float lineWidthScale = 0.06f; // 6% of height
    [Tooltip("dot radius = rect.height * dotRadiusScale")]
    public float dotRadiusScale = 0.10f;  // 10% of height
    [Tooltip("Minimum effective line width (in same units as rect)")]
    public float minLineWidth = 0.0005f;
    [Tooltip("Minimum effective dot radius")]
    public float minDotRadius = 0.0008f;

    // internal buffers
    List<float> rawBuffer;
    int writeIndex = 0;
    bool wrapped = false;

    protected override void OnEnable()
    {
        EnsureBuffer();
        SetAllDirty();
    }

    protected void OnValidate()
    {
        sampleCount = Mathf.Max(2, sampleCount);
        smoothingWindow = Mathf.Clamp(smoothingWindow, 1, sampleCount);
        lineWidth = Mathf.Max(0.001f, lineWidth);
        EnsureBuffer();
        SetAllDirty();
    }

    void EnsureBuffer()
    {
        if (rawBuffer == null || rawBuffer.Count != sampleCount)
        {
            rawBuffer = new List<float>(sampleCount);
            for (int i = 0; i < sampleCount; ++i) rawBuffer.Add(0f);
            writeIndex = 0;
            wrapped = false;
        }
    }

    /// <summary>
    /// Add a new sample (value in same units as fixedMinY/fixedMaxY, e.g. Watts)
    /// </summary>
    public void AddSample(float value)
    {
        EnsureBuffer();
        rawBuffer[writeIndex] = value;
        writeIndex++;
        if (writeIndex >= sampleCount) { writeIndex = 0; wrapped = true; }
        SetVerticesDirty();
    }

    /// <summary>
    /// Clear the sample buffer (set all samples to zero)
    /// </summary>
    public void ClearSamples()
    {
        EnsureBuffer();
        for (int i = 0; i < rawBuffer.Count; ++i) rawBuffer[i] = 0f;
        writeIndex = 0;
        wrapped = false;
        SetVerticesDirty();
    }

    // Returns chronological array (oldest first) padded to sampleCount length
    List<float> GetChronoBuffer()
    {
        EnsureBuffer();
        List<float> outList = new List<float>(sampleCount);
        if (!wrapped)
        {
            for (int i = 0; i < writeIndex; ++i) outList.Add(rawBuffer[i]);
            // pad with zeros at front so oldest is left and length == sampleCount
            while (outList.Count < sampleCount) outList.Insert(0, 0f);
        }
        else
        {
            for (int i = 0; i < sampleCount; ++i)
                outList.Add(rawBuffer[(writeIndex + i) % sampleCount]);
        }
        return outList;
    }

    // moving-average smoothing
    List<float> Smooth(List<float> data)
    {
        if (smoothingWindow <= 1) return data;
        int N = data.Count;
        List<float> sm = new List<float>(N);
        int half = smoothingWindow / 2;
        for (int i = 0; i < N; ++i)
        {
            int start = Mathf.Max(0, i - half);
            int end = Mathf.Min(N - 1, i + half);
            float sum = 0f; int cnt = 0;
            for (int j = start; j <= end; ++j) { sum += data[j]; cnt++; }
            sm.Add(sum / Mathf.Max(1, cnt));
        }
        return sm;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (rawBuffer == null || rawBuffer.Count < 2) return;

        Rect r = rectTransform.rect;
        var chrono = GetChronoBuffer();            // oldest..newest
        var smoothed = Smooth(chrono);

        // compute effective thicknesses
        float effLineWidth = lineWidth;
        float effDotRadius = latestDotRadius;
        if (autoScaleThickness)
        {
            float h = Mathf.Abs(r.height);
            effLineWidth = Mathf.Max(minLineWidth, h * lineWidthScale);
            effDotRadius = Mathf.Max(minDotRadius, h * dotRadiusScale);
        }

        // compute Y range
        float minY = fixedMinY, maxY = fixedMaxY;
        if (autoScaleY)
        {
            minY = float.MaxValue; maxY = float.MinValue;
            foreach (var v in smoothed) { if (v < minY) minY = v; if (v > maxY) maxY = v; }
            if (minY == float.MaxValue || maxY == float.MinValue) { minY = fixedMinY; maxY = fixedMaxY; }
            if (Mathf.Approximately(minY, maxY)) maxY = minY + 1f;
            float pad = (maxY - minY) * yPaddingPercent;
            minY -= pad; maxY += pad;
        }
        float invRange = 1f / (maxY - minY);

        int N = smoothed.Count;
        float width = r.width;
        float height = r.height;
        float step = N > 1 ? (width / (N - 1)) : width;

        // ---- draw grid lines first (under everything) ----
        if (drawGrid && gridHorizontalLines > 0)
        {
            UIVertex gv = UIVertex.simpleVert; gv.color = gridColor;
            for (int i = 0; i <= gridHorizontalLines; ++i)
            {
                float t = (float)i / gridHorizontalLines;
                float y = r.yMin + t * height;
                int startVert = vh.currentVertCount;
                // small thin quad as horizontal line
                Vector3 a = new Vector3(r.xMin, y - 0.0001f, 0f);
                Vector3 b = new Vector3(r.xMin, y + 0.0001f, 0f);
                Vector3 c = new Vector3(r.xMax, y + 0.0001f, 0f);
                Vector3 d = new Vector3(r.xMax, y - 0.0001f, 0f);
                UIVertex v1 = gv; v1.position = a; vh.AddVert(v1);
                UIVertex v2 = gv; v2.position = b; vh.AddVert(v2);
                UIVertex v3 = gv; v3.position = c; vh.AddVert(v3);
                UIVertex v4 = gv; v4.position = d; vh.AddVert(v4);
                vh.AddTriangle(startVert + 0, startVert + 1, startVert + 2);
                vh.AddTriangle(startVert + 0, startVert + 2, startVert + 3);
            }
        }

        // ---- build points (local rect coords) ----
        Vector2[] pts = new Vector2[N];
        for (int i = 0; i < N; ++i)
        {
            float x = r.xMin + i * step;
            float norm = Mathf.Clamp01((smoothed[i] - minY) * invRange);
            float y = r.yMin + norm * height;
            pts[i] = new Vector2(x, y);
        }

        // ---- filled area (optional) ----
        if (drawFilled)
        {
            int filledStart = vh.currentVertCount;
            UIVertex vert = UIVertex.simpleVert; vert.color = fillColor;
            vert.position = new Vector3(r.xMin, r.yMin, 0f);
            vh.AddVert(vert);
            for (int i = 0; i < pts.Length; ++i) { vert.position = pts[i]; vh.AddVert(vert); }
            vert.position = new Vector3(r.xMax, r.yMin, 0f);
            vh.AddVert(vert);
            // triangles (base = filledStart)
            int triCount = pts.Length + 1;
            for (int i = 0; i < triCount; ++i)
            {
                vh.AddTriangle(filledStart + i, filledStart + i + 1, filledStart + i + 2);
            }
        }

        // ---- polyline as quads between point pairs ----
        int lineStart = vh.currentVertCount;
        for (int i = 0; i < pts.Length - 1; ++i)
        {
            Vector2 a = pts[i], b = pts[i + 1];
            Vector2 delta = b - a;
            float len = delta.magnitude;
            if (len <= 1e-6f)
            {
                // degenerate: draw a tiny segment
                delta = Vector2.right * 1e-4f;
                len = delta.magnitude;
            }
            Vector2 dir = delta / len;
            Vector2 perp = new Vector2(-dir.y, dir.x) * (effLineWidth * 0.5f);

            UIVertex v1 = UIVertex.simpleVert; v1.color = lineColor; v1.position = a - perp;
            UIVertex v2 = UIVertex.simpleVert; v2.color = lineColor; v2.position = a + perp;
            UIVertex v3 = UIVertex.simpleVert; v3.color = lineColor; v3.position = b + perp;
            UIVertex v4 = UIVertex.simpleVert; v4.color = lineColor; v4.position = b - perp;

            vh.AddVert(v1); vh.AddVert(v2); vh.AddVert(v3); vh.AddVert(v4);

            int idx = lineStart + i * 4;
            vh.AddTriangle(idx + 0, idx + 1, idx + 2);
            vh.AddTriangle(idx + 0, idx + 2, idx + 3);
        }

        // ---- draw axes (left Y axis, bottom X axis) ----
        if (drawAxes)
        {
            float axisThickness = height * axisWidthScale; 
            UIVertex av = UIVertex.simpleVert; av.color = axisColor;

            // bottom axis (x)
            int axStart = vh.currentVertCount;
            Vector3 axBL = new Vector3(r.xMin, r.yMin - axisThickness * 0.5f, 0f);
            Vector3 axTL = new Vector3(r.xMin, r.yMin + axisThickness * 0.5f, 0f);
            Vector3 axTR = new Vector3(r.xMax, r.yMin + axisThickness * 0.5f, 0f);
            Vector3 axBR = new Vector3(r.xMax, r.yMin - axisThickness * 0.5f, 0f);
            av.position = axBL; vh.AddVert(av);
            av.position = axTL; vh.AddVert(av);
            av.position = axTR; vh.AddVert(av);
            av.position = axBR; vh.AddVert(av);
            vh.AddTriangle(axStart + 0, axStart + 1, axStart + 2);
            vh.AddTriangle(axStart + 0, axStart + 2, axStart + 3);

            // left axis (y)
            int ayStart = vh.currentVertCount;
            Vector3 ayBL = new Vector3(r.xMin - axisThickness * 0.5f, r.yMin, 0f);
            Vector3 ayTL = new Vector3(r.xMin - axisThickness * 0.5f, r.yMax, 0f);
            Vector3 ayTR = new Vector3(r.xMin + axisThickness * 0.5f, r.yMax, 0f);
            Vector3 ayBR = new Vector3(r.xMin + axisThickness * 0.5f, r.yMin, 0f);
            av.position = ayBL; vh.AddVert(av);
            av.position = ayTL; vh.AddVert(av);
            av.position = ayTR; vh.AddVert(av);
            av.position = ayBR; vh.AddVert(av);
            vh.AddTriangle(ayStart + 0, ayStart + 1, ayStart + 2);
            vh.AddTriangle(ayStart + 0, ayStart + 2, ayStart + 3);
        }

        // ---- draw time ticks on bottom axis (left/mid/right) ----
        if (drawTimeTicks)
        {
            DrawTick(vh, r.xMin, r.yMin, height, tickColor);
            DrawTick(vh, r.xMin + 0.5f * width, r.yMin, height, tickColor);
            DrawTick(vh, r.xMax, r.yMin, height, tickColor);
        }

        // ---- latest dot ----
        if (drawLatestDot && pts.Length > 0)
        {
            var latest = pts[pts.Length - 1];
            DrawDot(vh, latest, effDotRadius, latestDotColor);
        }
    }

    void DrawTick(VertexHelper vh, float x, float y0, float h, Color col)
    {
        UIVertex v = UIVertex.simpleVert; v.color = col;
        float tickH = Mathf.Min(8f, h * 0.12f);
        Vector3 a = new Vector3(x, y0, 0f), b = new Vector3(x, y0 + tickH, 0f);
        Vector3 a2 = a + Vector3.right * (tickH * 0.12f), b2 = b + Vector3.right * (tickH * 0.12f);
        v.position = a; vh.AddVert(v);
        v.position = b; vh.AddVert(v);
        v.position = b2; vh.AddVert(v);
        v.position = a2; vh.AddVert(v);
        int baseIdx = vh.currentVertCount - 4;
        vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
        vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
    }

    void DrawDot(VertexHelper vh, Vector2 pos, float radius, Color col)
    {
        UIVertex v = UIVertex.simpleVert; v.color = col;
        Vector3 bl = new Vector3(pos.x - radius, pos.y - radius, 0f);
        Vector3 tl = new Vector3(pos.x - radius, pos.y + radius, 0f);
        Vector3 tr = new Vector3(pos.x + radius, pos.y + radius, 0f);
        Vector3 br = new Vector3(pos.x + radius, pos.y - radius, 0f);
        v.position = bl; vh.AddVert(v);
        v.position = tl; vh.AddVert(v);
        v.position = tr; vh.AddVert(v);
        v.position = br; vh.AddVert(v);
        int baseIdx = vh.currentVertCount - 4;
        vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
        vh.AddTriangle(baseIdx, baseIdx + 2, baseIdx + 3);
    }

    /// <summary>
    /// Helper to mark the graphic dirty in both editor & play modes.
    /// </summary>
    public override void SetAllDirty()
    {
        SetVerticesDirty();
        SetMaterialDirty();
    }
}
