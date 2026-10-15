using System;
using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

public class Platform : MonoBehaviour
{
    public static Action<Platform> OnclickPlatform;
    [SerializeField] LayerMask platformLayer;
    public static bool OnPlatformOpen { get; set; } = false;
    private void Update()
    {
        if (OnPlatformOpen) return;
        CheckClick();
    }

    private void CheckClick()
    {
        if ( Input.GetMouseButtonDown(0))
        {
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D raycast = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, platformLayer);
            if ( raycast.collider != null)
            {
                
            
            Platform platform = raycast.collider.GetComponent<Platform>();
                if ( platform != null)
                {
                    OnclickPlatform?.Invoke(platform);
                }
            }
        }
    }

    public void BuildTower(TowerData data)
    {
        
            GameObject Tower = Instantiate(data.Prefab, transform.position, Quaternion.identity, transform);
            Tower.SetActive(true);
            GameManager.Instance.SpendGold(data.Cost);
        
       
        

    }

   
}
