using UnityEngine;

public class BulletFire : Bullet
{
    private const float BurnDurationSeconds = 3f;
    private const float BurnTickIntervalSeconds = 1f;

    private void Update()
    {
        FollowTarget();
        LookTarget();
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
            return;

        EnemyStatus targetEnemy = collision.GetComponentInParent<EnemyStatus>();
        if (targetEnemy != null && towerShoot != null)
        {
            int hitDamage = towerShoot.currentDame;

            // One direct hit, followed by delayed burn ticks. Repeated hits refresh
            // the burn on this enemy instead of stacking independent damage-over-time.
            targetEnemy.TakeDame(hitDamage);
            targetEnemy.ApplyBurn(hitDamage, BurnDurationSeconds, BurnTickIntervalSeconds);
        }

        // Consume the projectile on enemy contact even if its status component is missing.
        gameObject.SetActive(false);
    }
}
