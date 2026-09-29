using UnityEngine;
using UnityEngine.EventSystems;

public sealed class CoralUrchinTarget : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private CoralHarvestMinigame manager;
    private float removalRadius;
    private bool removed;

    public void Initialize(CoralHarvestMinigame targetManager, float coralEdgeRadius)
    {
        manager = targetManager;
        removalRadius = coralEdgeRadius;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (removed) return;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (removed || !(transform is RectTransform targetRect)) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        float scaleFactor = canvas != null ? canvas.scaleFactor : 1f;
        targetRect.anchoredPosition += eventData.delta / scaleFactor;

        if (targetRect.anchoredPosition.magnitude < removalRadius) return;

        removed = true;
        manager.OnUrchinDraggedOff(this);
    }
}
