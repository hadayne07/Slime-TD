using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private TowerData data;
    [SerializeField] private Transform currentTarget;
    private void Update()
    {
        FollowTarget();
    }

    protected void FollowTarget()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            currentTarget.position,
            data.Speed * Time.deltaTime
        );
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
}
