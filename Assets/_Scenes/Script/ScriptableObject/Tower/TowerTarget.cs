using System.Collections.Generic;
using UnityEngine;

public class TowerTarget : MonoBehaviour
{
    [SerializeField] private TowerData data;

    protected CircleCollider2D _collider;
    [SerializeField] public List<EnemyMove> enemiesInRange;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, data.Range);
    }

    private void Awake()
    {
        _collider = GetComponent<CircleCollider2D>();
        _collider.radius = data.Range;
    }

    // Dọn enemy đã chết/inactive khỏi danh sách mỗi frame
    private void Update()
    {
        enemiesInRange.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyMove enemy = collision.GetComponent<EnemyMove>();
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemiesInRange.Contains(enemy))
                enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyMove enemy = collision.GetComponent<EnemyMove>();
            if (enemy != null)
            {
                enemiesInRange.Remove(enemy);
            }
        }
    }
}
