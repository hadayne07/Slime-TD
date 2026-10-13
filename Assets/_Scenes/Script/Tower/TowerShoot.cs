using System;
using Unity.AI.Assistant;
using UnityEngine;

public class TowerShoot : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private ObjectPooler pooler;
    private Vector2 direction;
    private GameObject bullet;
    private TowerTarget towerTarget;
   [SerializeField] private float timeDelay= 0 ;

    private void Awake()
    {
        towerTarget = GetComponent<TowerTarget>();
    }
    public virtual void Shoot ()
    {

        timeDelay += Time.deltaTime;
        if (timeDelay < data.TimeShoot) return;
        timeDelay = 0f;
        bullet = pooler.GetPoolObj();
         bullet.transform.position = transform.position;
        bullet.SetActive(true);
        bullet.transform.position = Vector2.MoveTowards(bullet.transform.position, towerTarget.enemiesInRange[0].transform.position, data.Speed);
    }


 


}
