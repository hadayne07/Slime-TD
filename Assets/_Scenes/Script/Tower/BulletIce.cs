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
        // 1. XỬ LÝ LÀM CHẬM TRƯỚC
        if (collision.CompareTag("Enemy"))
        {
            // Lấy thành phần EnemyStatus từ quái vật bị trúng đạn
            EnemyStatus targetEnemy = collision.GetComponent<EnemyStatus>();

            if (targetEnemy != null)
            {
                // Gọi hàm kích hoạt làm chậm nằm trên chính quái vật
                targetEnemy.ApplySlowEffect(3f, 3f); // Làm chậm giảm 3 tốc độ, trong 3 giây
            }
        }

        // 2. HỦY VIÊN ĐẠN SAU CÙNG
        base.OnTriggerEnter2D(collision);
    }
}

