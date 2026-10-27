using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TowerClickSelector : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask towerClickMask;
    [SerializeField] private TowerPlacementManager placementManager;
    [SerializeField] private UIController uiController;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (worldCamera == null || uiController == null)
            return;

        // Không chọn tháp khi click vào UI.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        // Không chọn tháp trong lúc đang kéo để đặt tháp hoặc game đang bị khóa.
        if (placementManager != null && placementManager.IsPlacing)
            return;

        if (uiController.IsGameplayBlocked)
            return;

        Ray ray = worldCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        RaycastHit2D hit = Physics2D.GetRayIntersection(
            ray,
            Mathf.Infinity,
            towerClickMask
        );

        if (hit.collider == null)
            return;

        // Cho phép collider click nằm trên object con của tower.
        TowerShoot tower = hit.collider.GetComponentInParent<TowerShoot>();

        if (tower != null)
            uiController.SelectTower(tower);
    }
}
