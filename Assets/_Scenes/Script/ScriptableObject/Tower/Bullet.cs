using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] public TowerShoot towerShoot;
    [SerializeField] private Transform currentTarget;

    public EnemyStatus enemy;

    private EnemyStatus targetStatus;
    private bool hasHit;

    private void Awake()
    {
        if (towerShoot == null)
        {
            towerShoot = GetComponentInParent<TowerShoot>();
        }
    }

    protected virtual void OnEnable()
    {
        // Reset mỗi lần lấy đạn từ pool.
        hasHit = false;
        enemy = null;

        if (towerShoot != null)
        {
            transform.localScale =
                Vector3.one * towerShoot.sizeBullet;
        }
    }

    protected virtual void OnDisable()
    {
        currentTarget = null;
        targetStatus = null;
        enemy = null;
    }

    protected virtual void Update()
    {
        FollowTarget();

        // FollowTarget có thể đã tắt viên đạn.
        if (!isActiveAndEnabled)
            return;

        LookTarget();
    }

    public void SetTarget(Transform target)
    {
        currentTarget = target;

        targetStatus = target != null
            ? target.GetComponentInParent<EnemyStatus>()
            : null;
    }

    protected void FollowTarget()
    {
        // Mất mục tiêu, mục tiêu chết hoặc mất tháp sở hữu.
        if (towerShoot == null ||
            currentTarget == null ||
            !currentTarget.gameObject.activeInHierarchy ||
            (targetStatus != null && !targetStatus.IsAlive))
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget.position,
            Mathf.Max(0f, towerShoot.currentSpeed) * Time.deltaTime
        );
    }

    // Bullet thường, Ice và Fire đều dùng bước kiểm tra này.
    protected bool TryConsumeHit(
        Collider2D collision,
        out EnemyStatus targetEnemy)
    {
        targetEnemy = null;

        if (hasHit || !isActiveAndEnabled)
            return false;

        if (towerShoot == null)
        {
            gameObject.SetActive(false);
            return false;
        }

        targetEnemy =
            collision.GetComponentInParent<EnemyStatus>();

        if (targetEnemy == null || !targetEnemy.IsAlive)
            return false;

        // Khóa trước khi gây sát thương hoặc áp dụng hiệu ứng.
        hasHit = true;
        enemy = targetEnemy;

        return true;
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (!TryConsumeHit(
                collision,
                out EnemyStatus targetEnemy))
        {
            return;
        }

        targetEnemy.TakeDame(towerShoot.currentDame);

        gameObject.SetActive(false);
    }

    protected void LookTarget()
    {
        if (currentTarget == null)
            return;

        Vector2 direction =
            currentTarget.position - transform.position;

        if (direction.sqrMagnitude < 0.000001f)
            return;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}