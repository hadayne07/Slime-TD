using UnityEngine;

public class BulletFire : Bullet
{
    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (!TryConsumeHit(collision, out EnemyStatus targetEnemy))
            return;

        targetEnemy.TakeDame(towerShoot.currentDame);

        if (targetEnemy.IsAlive)
        {
            targetEnemy.ApplyBurn(
                towerShoot.currentDamageEffect,
                towerShoot.currentTimeEffect,
                1f
            );
        }

        gameObject.SetActive(false);
    }
}