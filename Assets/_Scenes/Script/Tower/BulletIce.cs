using UnityEngine;

public class BulletIce : Bullet
{
    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
            return;

        EnemyStatus targetEnemy = collision.GetComponentInParent<EnemyStatus>();
        if (targetEnemy == null)
            return;

        // Áp dụng slow trước, rồi gây sát thương cơ bản qua base
        targetEnemy.ApplySlowEffect(towerShoot.currentDamageEffect, towerShoot.currentTimeEffect);

        // Gây sát thương cơ bản và tắt bullet
        enemy = targetEnemy;
        enemy.TakeDame(towerShoot.currentDame);
        gameObject.SetActive(false);
    }
}
