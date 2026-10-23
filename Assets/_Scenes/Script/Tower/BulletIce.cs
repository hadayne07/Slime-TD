using System.Collections;
using UnityEngine;


public class BulletIce : Bullet
{
    private  float SlowPercent;
    private  float SlowDurationSeconds;

    

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

        targetEnemy.ApplySlowEffect(towerShoot.currentDamageEffect, towerShoot.currentTimeEffect);
        base.OnTriggerEnter2D(collision);
    }
}

