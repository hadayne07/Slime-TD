using System.Collections;
using UnityEngine;


public class BulletIce : Bullet
{
    private const float SlowPercent = 1f;
    private const float SlowDurationSeconds = 3f;

    private void Update()
    {
        FollowTarget();
        LookTarget();
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyStatus targetEnemy = collision.GetComponentInParent<EnemyStatus>();
        if (targetEnemy == null || !targetEnemy.CompareTag("Enemy"))
            return;

        targetEnemy.ApplySlowEffect(SlowPercent, SlowDurationSeconds);
        base.OnTriggerEnter2D(collision);
    }
}

