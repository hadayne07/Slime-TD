using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Platform : MonoBehaviour
{
    public static Action<Platform> OnclickPlatform;
    [SerializeField] LayerMask platformLayer;
    private void Update()
    {
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
}
