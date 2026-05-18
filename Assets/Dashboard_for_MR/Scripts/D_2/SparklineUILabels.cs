using TMPro;
using UnityEngine;

[ExecuteAlways]
public class SparklineUILabels : MonoBehaviour
{
    public SparklineUI spark;
    
    public TextMeshProUGUI xLeft;
    public TextMeshProUGUI xMid;
    public TextMeshProUGUI xRight;

    public TextMeshProUGUI yMin;
    public TextMeshProUGUI yMax;

    void Update()
    {
        if (!spark) return;

        Rect r = spark.rectTransform.rect;

        // ---- Y Labels ----
        yMin.text = spark.fixedMinY.ToString("0");
        yMax.text = spark.fixedMaxY.ToString("0");

        var yMinRT = yMin.rectTransform;
        var yMaxRT = yMax.rectTransform;

        //yMinRT.anchoredPosition = new Vector2(r.xMin - 10f, r.yMin);
        //yMaxRT.anchoredPosition = new Vector2(r.xMin - 10f, r.yMax);

        // ---- X Labels (time) ----
        xLeft.text  = "0s";
        xMid.text   = (spark.sampleCount * 0.5f).ToString("0") + "s";
        xRight.text = (spark.sampleCount).ToString("0") + "s";

        var xl = xLeft.rectTransform;
        var xm = xMid.rectTransform;
        var xr = xRight.rectTransform;

        //xl.anchoredPosition = new Vector2(r.xMin, r.yMin - 20f);
        //xm.anchoredPosition = new Vector2(r.xMin + r.width * 0.5f, r.yMin - 20f);
        //xr.anchoredPosition = new Vector2(r.xMax, r.yMin - 20f);
    }
}
