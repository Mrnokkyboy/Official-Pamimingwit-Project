using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CoralUrchinTarget : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private CoralHarvestMinigame manager;
    private float removalRadius;
    private bool removed;
    private Vector2 previousPointerPosition;

    public void Initialize(CoralHarvestMinigame targetManager, float coralEdgeRadius)
    {
        manager = targetManager;
        removalRadius = coralEdgeRadius;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (removed) return;
        transform.SetAsLastSibling();
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
        if (manager.OnUrchinDragSpeed(speed, elapsedTime))
        {
            removed = true;
            return;
        }

        if (targetRect.anchoredPosition.magnitude < removalRadius) return;

        removed = true;
        manager.OnUrchinDraggedOff(this);
    }
}
