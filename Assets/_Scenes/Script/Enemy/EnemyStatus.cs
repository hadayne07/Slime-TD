using System;
using System.Collections;
using UnityEditor.PackageManager;
using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
    [SerializeField]private EnemyData data;
    public static Action<EnemyStatus> enemyDie;
    public static Action<EnemyData> goldplus;
    [SerializeField] private float hp;
 
    [SerializeField] private Transform healthbar;
    private Vector3 healthBarOriginalScale;
    private void OnEnable()
    {
        hp = data.hp;
       
        UpdateHealthBar();
  
    }

    private void Awake()
    {
        healthBarOriginalScale = healthbar.localScale;
    }


    private void Update()
    {
         if (hp <= 0)
        {
            goldplus?.Invoke(data);
            enemyDie?.Invoke(this);
            gameObject.SetActive(false);
        }
    }

    public void TakeDame(int dame)

    {
        hp -= dame;
        UpdateHealthBar();

    }

    protected void UpdateHealthBar()
    {
        float healthBarPercent = hp / data.hp;
        Vector3 scale = healthBarOriginalScale;
        scale.x = healthBarOriginalScale.x * healthBarPercent;
        healthbar.localScale = scale;
    }

    public void HasSlowed ( float slow)
    {
        data.speed -= slow;
    }
    public void SetSpeed (float slow)
    {
        data.speed += slow;
    }
    public IEnumerator BurnRoutine(float duration, int damage)
    {

        float timer = 0f;

        while (timer < duration && hp > 0)
        {
            TakeDame(damage);

            yield return new WaitForSeconds(1f);
            timer += 1f;
        }



    }
    // Thêm biến này lên đầu class EnemyStatus để kiểm tra tránh trùng lặp hiệu ứng làm chậm nếu trúng nhiều đạn băng
    private bool isSlowed = false;

    // Hàm này bạn thêm vào cuối class EnemyStatus nhé:
    public void ApplySlowEffect(float slowAmount, float duration)
    {
        // Nếu chưa bị làm chậm thì mới bắt đầu chạy Coroutine đếm giờ
        if (!isSlowed)
        {
            StartCoroutine(SlowRoutine(slowAmount, duration));
        }
    }

    private IEnumerator SlowRoutine(float slowAmount, float duration)
    {
        isSlowed = true;

       
        HasSlowed(slowAmount);
        Debug.Log(gameObject.name + " bị làm chậm!");

        float timer = 0f;
    
        while (timer < duration && hp > 0)
        {
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }


        if (hp > 0)
        {
            SetSpeed(slowAmount);
            Debug.Log(gameObject.name + " đã hết bị làm chậm.");
        }

        isSlowed = false;
    }


}
