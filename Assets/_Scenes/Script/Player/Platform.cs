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
        if ( Input.GetMouseButtonDown(0))
        {
            Debug.Log("Click");
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D raycast = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, platformLayer);
            if ( raycast.collider != null)
            {
                
            
            Platform platform = raycast.collider.GetComponent<Platform>();
                if ( platform != null)
                {
                    OnclickPlatform?.Invoke(platform);
                    Debug.Log("OnclickPlatform");
                }
            }
        }
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
