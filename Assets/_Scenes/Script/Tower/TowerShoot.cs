using UnityEngine;

public class TowerShoot : MonoBehaviour
{
    [SerializeField] private TowerData data;
    public TowerData Data { get => data; }
    [SerializeField] private ObjectPooler pooler;

    
    private TowerTarget target;

    private void Awake()
    {
        target = GetComponent<TowerTarget>();
    }

    public virtual void Shoot()
    {
        if(target.enemiesInRange.Count >0)
        {
            GameObject bullet = pooler.GetPoolObj();
            bullet.transform.position = transform.position;
            bullet.SetActive(true);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetTarget(target.enemiesInRange[0].transform);
            }
            Debug.Log("shoot");
         
        }
    }
}
