using System.Collections;
using UnityEngine;

public class BulletFire : Bullet
{
    private bool isBurning = false;
    private void Update()
    {
        FollowTarget();
        LookTarget();

    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.CompareTag("Enemy"))
        {
            StartCoroutine(BurnRoutine(3f));
        }
    }

    private IEnumerator BurnRoutine(float duration)
    {
        isBurning = true;
        float timer = 0f;

        while (timer < duration)
        {
            enemy.TakeDame(towerShoot.currentDame);

            yield return new WaitForSeconds(1f); 
            timer += 1f;
        }

        isBurning = false;
        
    }
}
