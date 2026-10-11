using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TowerTarget : MonoBehaviour
{
    [SerializeField] private TowerData data;
    protected CircleCollider2D collider2D;
    [SerializeField] private List<EnemyMove> enemiesInRange;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere (transform.position, data.Range);
    }

    private void Awake()
    {
        collider2D = GetComponent<CircleCollider2D>();
        collider2D.radius = data.Range;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyMove enemy = collider2D.GetComponent<EnemyMove>();
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyMove enemy = collider2D.GetComponent<EnemyMove>();
            if (enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Remove(enemy);
            }
        }
    }

}
