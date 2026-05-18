using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RoundedPanelController : MonoBehaviour
{
    [Tooltip("Corner radius in pixels")]
    public float radiusPixels = 12f;

    [Tooltip("AA smoothness in pixels")]
    public float smoothPx = 1.5f;

    Image img;
    Material matInstance;

    void Awake()
    {
        img = GetComponent<Image>();
        if (img.material == null)
        {
            Debug.LogError("[RoundedPanelController] Assign a material using the 'UI/RoundedPanelPixel' shader to the Image first.");
            enabled = false;
            return;
        }

        // Instantiate material so we don't modify the shared asset
        matInstance = Instantiate(img.material);
        img.material = matInstance;
    }

    void Start()
    {
        UpdateMaterialProperties();
    }

    void OnEnable()
    {
        // Ensure properties are updated when enabled
        UpdateMaterialProperties();
    }

    void Update()
    {
        // If the rect size or radius can change at runtime, update each frame or only when needed.
        UpdateMaterialProperties();
    }

    void UpdateMaterialProperties()
    {
        if (matInstance == null || img == null) return;

        RectTransform rt = img.rectTransform;
        float w = rt.rect.width;
        float h = rt.rect.height;

        // Avoid zero-sized rects
        if (w <= 0 || h <= 0) return;

        matInstance.SetVector("_RectSize", new Vector4(w, h, 0, 0));
        matInstance.SetFloat("_RadiusPx", Mathf.Max(0f, radiusPixels));
        matInstance.SetFloat("_SmoothPx", Mathf.Max(0.1f, smoothPx));
    }

    void OnDestroy()
    {
        // Cleanup instantiated material
        if (matInstance != null)
        {
            Destroy(matInstance);
        }
    }
}
