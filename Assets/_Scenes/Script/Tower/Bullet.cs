using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Bullet : MonoBehaviour
{
    [SerializeField] public TowerShoot towerShoot;
    [SerializeField] private Transform currentTarget;
    public EnemyStatus enemy;


    private void Awake()
    {
        towerShoot = GetComponentInParent<TowerShoot>();
        transform.localScale = Vector3.one * towerShoot.sizeBullet;

    }
    private void Update()
    {
        FollowTarget();
        LookTarget();
       
    }

    protected void FollowTarget()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position,  currentTarget.position, towerShoot.currentSpeed  * Time.deltaTime);
    }
    public void SetTarget( Transform target)
    {
        currentTarget = target;
       
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if ( collision.CompareTag("Enemy"))
        {
           enemy = collision.GetComponent<EnemyStatus>();
            enemy.TakeDame(towerShoot.currentDame);
     
            gameObject.SetActive(false);
        }
    }
    protected void LookTarget()
    {
        if (currentTarget == null) return;

        Vector2 direction = currentTarget.position - transform.position;
        if (direction.sqrMagnitude < 0.1f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

   
}
