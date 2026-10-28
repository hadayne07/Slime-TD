using UnityEngine;

public class BulletIce : Bullet
{

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (!TryConsumeHit(collision, out EnemyStatus targetEnemy))
            return;

        targetEnemy.TakeDame(towerShoot.currentDame);

        if (targetEnemy.IsAlive)
        {
            targetEnemy.ApplySlowEffect( towerShoot.currentDamageEffect,towerShoot.currentTimeEffect );
        }

        gameObject.SetActive(false);
    }
}