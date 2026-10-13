using UnityEngine;

public class TowerCtrl : MonoBehaviour
{
    private TowerShoot towerShoot;
    private TowerTarget towerTarget;
    private void Awake()
    {
        towerShoot = GetComponent<TowerShoot>();
        towerTarget = GetComponent<TowerTarget>();
    }

    private void Update()
    {
        if (towerTarget.enemiesInRange.Count > 0)
            towerShoot.Shoot();
    }
}
