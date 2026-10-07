using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CoralUrchinTarget : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private CoralHarvestMinigame manager;
    private float removalRadius;
    private bool removed;
    private bool dragging;
    private bool safeDrag = true;
    private Vector2 previousPointerPosition;
    private Vector3 initialScale;
    private Image targetImage;
    private Color initialColor;
    private Color safeDragColor;
    private Color unsafeDragColor;

    public void Initialize(CoralHarvestMinigame targetManager, float coralEdgeRadius, Color safeColor, Color unsafeColor)
    {
        manager = targetManager;
        removalRadius = coralEdgeRadius;
        initialScale = transform.localScale;
        targetImage = GetComponentInChildren<Image>();
        if (targetImage != null) initialColor = targetImage.color;
        safeDragColor = safeColor;
        unsafeDragColor = unsafeColor;
    }

    private void Update()
    {
        if (removed) return;

        float targetScale = dragging ? (safeDrag ? 1.16f : 1.28f) : 1f;
        float blend = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, initialScale * targetScale, blend);
        if (targetImage != null)
        {
            Color feedbackColor = dragging ? (safeDrag ? safeDragColor : unsafeDragColor) : initialColor;
            targetImage.color = Color.Lerp(targetImage.color, feedbackColor, blend);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (removed) return;
        transform.SetAsLastSibling();
        dragging = true;
        safeDrag = true;
        previousPointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (removed || !(transform is RectTransform targetRect)) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        targetRect.anchoredPosition += eventData.delta / scaleFactor;

        float elapsedTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float speed = Vector2.Distance(previousPointerPosition, eventData.position) / scaleFactor / elapsedTime;
        previousPointerPosition = eventData.position;
        if (manager.OnUrchinDragSpeed(speed, elapsedTime, this))
        {
            removed = true;
            return;
        }

        if (targetRect.anchoredPosition.magnitude < removalRadius) return;

        removed = true;
        manager.OnUrchinDraggedOff(this);
    }

    public void SetDragFeedback(bool inSafeRange)
    {
        safeDrag = inSafeRange;
    }
}
