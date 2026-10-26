using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TowerCard : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private Image towerImage;
    [SerializeField] private TMP_Text cost;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    [SerializeField] private TowerPlacementManager placementManager;

    [SerializeField] private TowerData towerData;

    private void Start()
    {
        if (towerData == null)
        {
            Debug.LogWarning($"Chưa gán TowerData cho {gameObject.name}", this);
            return;
        }

        Initialize(towerData);
    }
    public void Initialize(TowerData data)
    {
        towerData = data;

        towerImage.sprite = data.Spritelv1;
        cost.text = data.Cost.ToString();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (towerData == null || placementManager == null)
            return;

        placementManager.BeginPlacement(towerData);
        placementManager.UpdatePlacement(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (placementManager == null) return;

        placementManager.UpdatePlacement(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (placementManager == null)
            return;

        bool isOverUI = IsPointerOverUI(eventData);

        placementManager.ConfirmPlacement( eventData.position, isOverUI );
    }

    private bool IsPointerOverUI(PointerEventData eventData)
    {
        if (EventSystem.current == null)
            return false;

        uiHits.Clear();

        EventSystem.current.RaycastAll(eventData, uiHits);

        foreach (RaycastResult hit in uiHits)
        {
            if (hit.module is GraphicRaycaster)
            {
                return true;
            }
        }

        return false;
    }
}