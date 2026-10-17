
using UnityEngine;

public class BulletFire : Bullet
{

    private void Update()
    {
        FollowTarget();
        LookTarget();

    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyStatus targetEnemy = collision.GetComponent<EnemyStatus>();

            if (targetEnemy != null)
            {

                targetEnemy.StartCoroutine(targetEnemy.BurnRoutine(3f, towerShoot.currentDame));
            }
        }

        base.OnTriggerEnter2D(collision);
        

    }

    
}
