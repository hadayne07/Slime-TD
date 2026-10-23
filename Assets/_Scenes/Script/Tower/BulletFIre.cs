using UnityEngine;

public class BulletFire : Bullet
{
   

    private  float BurnTickIntervalSeconds = 1f;

    
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

           
            targetEnemy.TakeDame(hitDamage);
            targetEnemy.ApplyBurn(towerShoot.currentDamageEffect, towerShoot.currentTimeEffect, BurnTickIntervalSeconds);
        }


        gameObject.SetActive(false);
    }
}
