using UnityEngine;

public class TowerCtrl : MonoBehaviour
{

    private TowerTarget towerTarget;
    private TowerShoot towershoot;
   [SerializeField] private float timeDelay = 0f;
    private void Awake()
    {
        towershoot = GetComponent<TowerShoot>();
         towerTarget = GetComponent<TowerTarget>();
    }

    private void Update()
    {
        timeDelay += Time.deltaTime;
        if (timeDelay < towershoot.Data.TimeShoot) return;
        timeDelay = 0f;
        towershoot.Shoot();

    }
}
