using System;
using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

public class Platform : MonoBehaviour
{
    public static Action<Platform> OnclickPlatform;
    [SerializeField] LayerMask platformLayer;
    private TowerShoot towerInstance;
    public TowerShoot TowerInstance => towerInstance;
    public static bool OnPlatformOpen { get; set; } = false;
    private void Update()
    {
        if (OnPlatformOpen || Time.timeScale == 0f) return;
        CheckClick();
    }

   
    private void CheckClick()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        Vector2 screenPosition = pointer.position.ReadValue();
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hitCollider = Physics2D.OverlapPoint(worldPoint, platformLayer);
        if (hitCollider == null)
            return;

        Platform platform = hitCollider.GetComponentInParent<Platform>();
        if (platform != null)
            OnclickPlatform?.Invoke(platform);
    }

    public void BuildTower(TowerData data)
    {

        if (data == null || data.Prefab == null || towerInstance != null) return;
        GameObject Tower = Instantiate(data.Prefab, transform.position, Quaternion.identity, transform);
        towerInstance = Tower.GetComponentInChildren<TowerShoot>(true);
            Tower.SetActive(true);
            GameManager.Instance.SpendGold(data.Cost);
        
    }

   
}
