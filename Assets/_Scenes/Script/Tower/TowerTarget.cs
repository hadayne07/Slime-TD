using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TowerTarget : MonoBehaviour
{
    [SerializeField] private TowerData data;
    
    protected CircleCollider2D _collider;
    [SerializeField] public List<EnemyMove> enemiesInRange;
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere (transform.position, data.Range);
    }

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _collider.radius = data.Range/3;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyMove enemy = collision.GetComponent<EnemyMove>();
            enemiesInRange.Add(enemy);
      
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyMove enemy = collision.GetComponent<EnemyMove>();
            if (enemiesInRange.Contains(enemy))
            {
                enemiesInRange.Remove(enemy);
            }
        }
    }

}
