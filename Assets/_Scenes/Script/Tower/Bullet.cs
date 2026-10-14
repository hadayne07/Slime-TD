using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Bullet : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private Transform currentTarget;

    private void Start()
    {
        transform.localScale = Vector3.one * data.BulletSize;
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

        transform.position = Vector2.MoveTowards(transform.position,  currentTarget.position,  data.Speed * Time.deltaTime);
    }
    public void SetTarget( Transform target)
    {
        currentTarget = target;
       
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ( collision.CompareTag("Enemy"))
        {
            EnemyStatus enemy = collision.GetComponent<EnemyStatus>();
            enemy.TakeDame(data.Damage);
     
            gameObject.SetActive(false);
        }
    }
    protected void LookTarget()
    {
        if (currentTarget == null) return;

        Vector2 direction = currentTarget.position - transform.position;
        if (direction.sqrMagnitude < 0.000001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
