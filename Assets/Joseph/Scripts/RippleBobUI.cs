using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class RippleBobUI : MonoBehaviour
{
    [Header("Ripple Settings (match shader)")]
    [Tooltip("Target RectTransform acting as the center source of the wave propagation.")]
    [SerializeField] private RectTransform rippleCenter;
    [SerializeField] private float rippleSpeed = 1f;
    [SerializeField] private float rippleScale = 0.05f;
    [SerializeField] private float rippleStrength = 10f;
    [Tooltip("If true, uses Time.unscaledTime so UI bobbing continues while the game is paused.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Shadow")]
    [SerializeField] private RectTransform shadow;
    [SerializeField] private float shadowDelay = 0.2f;
    [SerializeField] private float shadowStrengthMultiplier = 0.4f;

    [Header("Extra Motion")]
    [SerializeField] private bool useRotation = true;
    [SerializeField] private float rotationAmount = 3f;

    [SerializeField] private bool useScale = true;
    [SerializeField] private float scaleAmount = 0.05f;

    private RectTransform rt;
    private Vector2 startPos;
    private Vector2 shadowStartPos;
    private Image shadowImage;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        CachePositions();
    }

    private void OnEnable()
    {
        // Re-anchor start position when object reactivates to prevent canvas/layout shifts
        CachePositions();
    }

    /// <summary>
    /// Recalculates and caches origin positions for the UI element and shadow.
    /// Call this if layout rebuilding or canvas resizing changes initial coordinates.
    /// </summary>
    public void CachePositions()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;

        if (shadow != null)
        {
            shadowStartPos = shadow.anchoredPosition;
            if (shadowImage == null)
            {
                shadowImage = shadow.GetComponent<Image>();
            }
        }
    }

    private void Update()
    {
        if (rippleCenter == null) return;

        // Sample time base
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;

        // Sample distance using immutable startPos to prevent positional drift
        float distance = Vector2.Distance(startPos, rippleCenter.anchoredPosition);

        // Main Wave Waveform Calculation
        float wave = Mathf.Sin(distance * rippleScale - time * rippleSpeed);

        // Position Updates
        rt.anchoredPosition = startPos + Vector2.up * (wave * rippleStrength);

        // Rotation Matrix Updates
        if (useRotation)
        {
            float rot = wave * rotationAmount;
            rt.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        // Scale Factor Updates
        if (useScale)
        {
            float scale = 1f + wave * scaleAmount;
            rt.localScale = new Vector3(scale, scale, 1f);
        }

        // Shadow Processing Loop
        if (shadow != null)
        {
            float shadowWave = Mathf.Sin(distance * rippleScale - (time - shadowDelay) * rippleSpeed);

            shadow.anchoredPosition = shadowStartPos + Vector2.up * (shadowWave * (rippleStrength * 0.15f * shadowStrengthMultiplier));

            float shadowScale = 1f - shadowWave * 0.08f;
            shadow.localScale = new Vector3(shadowScale * 1.2f, shadowScale * 0.7f, 1f);

            if (shadowImage != null)
            {
                Color color = shadowImage.color;
                color.a = 0.25f + (1f - Mathf.Abs(shadowWave)) * 0.25f;
                shadowImage.color = color;
            }
        }
    }

    [ContextMenu("Recalculate Anchor Positions")]
    private void RecalculateAnchorsEditor()
    {
        CachePositions();
        Debug.Log($"[RippleBobUI] Re-anchored starting position to: {startPos}");
    }
}