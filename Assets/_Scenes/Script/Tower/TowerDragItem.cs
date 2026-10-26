using UnityEngine;
using UnityEngine.EventSystems;

public class TowerDragItem : MonoBehaviour,

    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private TowerData data;
    [SerializeField] private TowerPlacementManager placementManager;

    public void OnBeginDrag (PointerEventData eventData)
    {
        placementManager.BeginPlacement(data);
        placementManager.UpdatePlacement(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        placementManager.UpdatePlacement(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        placementManager.CancelPlacement();
    }
}



