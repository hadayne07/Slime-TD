using System.Collections;
using UnityEngine;

public class BulletIce : Bullet
{
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
            StartCoroutine(SlowRoutine());
        }
    }

    private IEnumerator SlowRoutine()
    {
        enemy.HasSlowed(3f);
        yield return new WaitForSeconds(3f);
        enemy.SetSpeed(3f);
           
    }
}
