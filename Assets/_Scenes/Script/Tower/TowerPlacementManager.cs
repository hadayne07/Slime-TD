using System;
using UnityEngine;

public class TowerPlacementManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private float mapZ = 0f;

    [Header("Layer kiểm tra")]
    [SerializeField] private LayerMask buildableMask;
    [SerializeField] private LayerMask blockedMask;

    [Header("Màu preview")]
    [SerializeField] private Color validColor = new Color(0.5f, 1f, 0.5f, 0.65f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.4f, 0.4f, 0.65f);

    [Header("Hiển thị tầm bắn")]
    [SerializeField] private Material rangeMaterial;
    [SerializeField] private float rangeLineWidth = 0.04f;
    [SerializeField] private int rangeSegments = 96;

    private LineRenderer rangeLine;
    private TowerData selectedTower;
    private GameObject preview;
    private PolygonCollider2D previewFootprint;
    private SpriteRenderer[] previewRenderers;
    public static Action<TowerData> CantBuildTower;
    private ContactFilter2D buildableFilter;
    private ContactFilter2D blockedFilter;
    public bool IsPlacing => preview != null && selectedTower != null;


    // Chỉ cần biết có ít nhất một collider chạm vào hay không.
    private readonly Collider2D[] overlapResults = new Collider2D[1];

    private bool canPlace;

    private void Awake()
    {
        buildableFilter = new ContactFilter2D();
        buildableFilter.SetLayerMask(buildableMask);
        buildableFilter.useTriggers = true;

        blockedFilter = new ContactFilter2D();
        blockedFilter.SetLayerMask(blockedMask);
        blockedFilter.useTriggers = true;
    }

    public void BeginPlacement(TowerData data)
    {
        if (UIController.Instance != null && UIController.Instance.IsGameplayBlocked)
        {
            CancelPlacement();
            return;
        }
        CancelPlacement();

        if (data == null || data.placementPreviewPrefab == null)
        {
            Debug.LogWarning("Chưa gán prefab preview cho TowerData.");
            return;
        }
        selectedTower = data;
        preview = Instantiate(data.placementPreviewPrefab);
        preview.SetActive(true);

        previewFootprint = preview.GetComponentInChildren<PolygonCollider2D>();

        previewRenderers = preview.GetComponentsInChildren<SpriteRenderer>();

        if (previewFootprint == null)
        {
            Debug.LogWarning("Preview thiếu PolygonCollider2D ở chân tháp.");
            CancelPlacement();
            return;
        }
        CreateRangeCircle(data.Range);
        SetPreviewColor(false);
    }

    public void UpdatePlacement(Vector2 screenPosition)
    {
        if (preview == null || worldCamera == null)
            return;

        Ray ray = worldCamera.ScreenPointToRay(screenPosition);

        Plane mapPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, mapZ));

        if (!mapPlane.Raycast(ray, out float distance))
        {
            SetPreviewColor(false);
            canPlace = false;
            return;
        }

        Vector3 newPosition = ray.GetPoint(distance);
        Vector3 movement = newPosition - preview.transform.position;

        preview.transform.position = newPosition;

        if (rangeLine != null)
        {
            for (int i = 0; i < rangeLine.positionCount; i++)
            {
                Vector3 oldPoint = rangeLine.GetPosition(i);

                rangeLine.SetPosition(i, oldPoint + movement);
            }
        }

        // Cập nhật vị trí collider trước khi kiểm tra vật lý.
        Physics2D.SyncTransforms();

        canPlace = CheckPlacementPosition();
        SetPreviewColor(canPlace);
    }

    private bool CheckPlacementPosition()
    {
        // Lấy tâm hộp bao quanh footprint, không lấy tâm sprite.
        Vector2 footprintCenter = previewFootprint.bounds.center;

        int groundCount = Physics2D.OverlapPoint(footprintCenter, buildableFilter, overlapResults);

        if (groundCount == 0)
            return false;

        // Kiểm tra bằng hình PolygonCollider2D thực tế.
        int blockedCount = previewFootprint.Overlap(blockedFilter, overlapResults);
        if (blockedCount > 0)
            return false;

        return true;
    }

    private void SetPreviewColor(bool valid)
    {
        Color color = valid ? validColor : invalidColor;

        if (previewRenderers != null)
        {
            foreach (SpriteRenderer spriteRenderer in previewRenderers)
            {
                spriteRenderer.color = color;
            }
        }

        if (rangeLine != null)
        {
            rangeLine.startColor = color;
            rangeLine.endColor = color;
        }
    }

    public void CancelPlacement()
    {
        if (preview != null)
        {
            Destroy(preview);
        }

        preview = null;
        previewFootprint = null;
        previewRenderers = null;
        canPlace = false;
        rangeLine = null;
        selectedTower = null;
    }

    private void CreateRangeCircle(float radius)
    {
        GameObject rangeObject = new GameObject("RangePreview");

        rangeObject.transform.SetParent(preview.transform, false);

        rangeLine = rangeObject.AddComponent<LineRenderer>();

        rangeLine.sharedMaterial = rangeMaterial;
        rangeLine.useWorldSpace = true;
        rangeLine.loop = true;

        rangeLine.widthMultiplier = rangeLineWidth;

        rangeLine.numCornerVertices = 2;
        rangeLine.numCapVertices = 2;

        // Hiển thị cùng Sorting Layer với hình preview,
        // nhưng có Order cao hơn.
        if (previewRenderers.Length > 0)
        {
            rangeLine.sortingLayerID = previewRenderers[0].sortingLayerID;

            rangeLine.sortingOrder = previewRenderers[0].sortingOrder + 1;
        }

        int segments = Mathf.Max(12, rangeSegments);
        rangeLine.positionCount = segments;
        Vector3 center = preview.transform.position;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            rangeLine.SetPosition(i, point);
        }
    }

    public void ConfirmPlacement(Vector2 screenPosition, bool isOverUI)
    {
        if (UIController.Instance != null && UIController.Instance.IsGameplayBlocked)
        {
            CancelPlacement();
            return;
        }
        if (preview == null || selectedTower == null)
        {
            CancelPlacement();
            return;
        }

        // Không xây nếu thả trên bảng chọn hoặc nút UI.
        if (isOverUI)
        {
            CancelPlacement();
            return;
        }

        // Kiểm tra lại tại đúng vị trí thả chuột.
        UpdatePlacement(screenPosition);

        if (!canPlace)
        {
            CancelPlacement();
            return;
         
        }

        if (selectedTower.Prefab == null)
        {
            Debug.LogWarning("TowerData chưa được gán prefab tháp thật.");
            CancelPlacement();
            return;
        }

        // Kiểm tra đủ vàng trước khi xây — dùng TrySpendGold thay vì SpendGold
        if (!GameManager.Instance.TrySpendGold(selectedTower.Cost))
        {
            CancelPlacement();
            CantBuildTower?.Invoke(selectedTower);
            return;
        }

        GameObject tower = Instantiate(
            selectedTower.Prefab,
            preview.transform.position,
            preview.transform.rotation
        );

        tower.SetActive(true);

        // Đồng bộ collider tháp vừa tạo để lần đặt sau nhận biết.
        Physics2D.SyncTransforms();
        CancelPlacement();
    }
}
