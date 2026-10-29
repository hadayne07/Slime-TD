using System;
using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
    [Header("Dữ liệu")]
    [SerializeField] private EnemyData data;

    public static Action<EnemyStatus> enemyDie;
    public static Action<EnemyData> goldplus;

    [Header("Máu")]
    [SerializeField] private float hp;
    private float maxHp;
    private float waveHpMultiplier = 1f;

    [Header("Thanh máu")]
    [SerializeField] private Transform healthbar;
    private Vector3 healthBarOriginalScale;

    [Header("Di chuyển")]
    public float currentSpeed;

    private bool isDead;

    public bool IsAlive =>!isDead && hp > 0f && isActiveAndEnabled;

    // Slow theo phần trăm.
    private const float MaxSlowPercent = 0.4f;
    private float activeSlowPercent;
    private float slowExpiresAt;

    // Burn.
    private bool isBurning;

    private double burnExpiresAt;
    private double nextBurnTickAt;

    private float burnTickInterval = 1f;
    private float burnDamage;

    // Sai số nhỏ khi so sánh mốc thời gian.
    private const double BurnTimeEpsilon = 0.000001;

    private void Awake()
    {
        if (healthbar != null)
        {
            healthBarOriginalScale = healthbar.localScale;
        }
    }

    private void OnEnable()
    {
        if (data == null)
        {
            Debug.LogError("Enemy chưa được gán EnemyData.", this);
            gameObject.SetActive(false);
            return;
        }

        maxHp = Mathf.Max(
            1f,
            Mathf.Ceil(data.Hp * waveHpMultiplier)
        );

        hp = maxHp;
        isDead = false;

        currentSpeed = data.Speed;
        activeSlowPercent = 0f;
        slowExpiresAt = 0f;

        ResetBurn();

        UpdateHealthBar();
    }

    private void Update()
    {
        if (!IsAlive || Time.deltaTime <= 0f)
            return;

        UpdateSlow();
        UpdateBurn();
    }
    private void OnDisable()
    {
        ResetBurn();

        activeSlowPercent = 0f;
        slowExpiresAt = 0f;

        if (data != null)
        {
            currentSpeed = data.Speed;
        }
    }

    // Wave 1–10: HP cơ bản.
    // Từ Wave 11: mỗi Wave tăng thêm 10% HP cơ bản.
    public void PrepareForWave(int waveNumber)
    {
        waveHpMultiplier =
            1f + Mathf.Max(0, waveNumber - 10) * 0.03f;
    }

    public void TakeDame(float dame)
    {
        if (!IsAlive || dame <= 0f)
            return;

        hp = Mathf.Max(0f, hp - dame);
        UpdateHealthBar();

        if (hp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        goldplus?.Invoke(data);
        enemyDie?.Invoke(this);

        gameObject.SetActive(false);
    }

    protected void UpdateHealthBar()
    {
        if (healthbar == null || maxHp <= 0f)
            return;

        float healthBarPercent = Mathf.Clamp01(hp / maxHp);

        Vector3 scale = healthBarOriginalScale;
        scale.x = healthBarOriginalScale.x * healthBarPercent;

        healthbar.localScale = scale;
    }

    // ================= SLOW =================

    public void ApplySlowEffect(float slowPercent, float duration)
    {
        if (!IsAlive || duration <= 0f)
            return;

        slowPercent = Mathf.Clamp(
            slowPercent,
            0f,
            MaxSlowPercent
        );

        if (slowPercent <= 0f)
            return;

        if (Time.time >= slowExpiresAt)
        {
            activeSlowPercent = 0f;
        }

        // Lấy mức mạnh nhất, không cộng dồn.
        activeSlowPercent = Mathf.Max(
            activeSlowPercent,
            slowPercent
        );

        // Đòn mới có thể kéo dài hiệu ứng.
        slowExpiresAt = Mathf.Max(
            slowExpiresAt,
            Time.time + duration
        );

        currentSpeed = data.Speed * (1f - activeSlowPercent);
    }

    private void UpdateSlow()
    {
        if (activeSlowPercent <= 0f)
            return;

        if (Time.time >= slowExpiresAt)
        {
            activeSlowPercent = 0f;
            slowExpiresAt = 0f;

            currentSpeed = data.Speed;
        }
    }

    // ================= BURN =================

    public void ApplyBurn(
     float damage,
     float duration,
     float tickInterval = 1f)
    {
        if (!IsAlive || damage <= 0f || duration <= 0f)
            return;

        // Xử lý các tick đã đến hạn trước khi nhận burn mới.
        UpdateBurn();

        // Tick burn vừa xử lý có thể đã giết quái.
        if (!IsAlive)
            return;

        double now = Time.timeAsDouble;

        if (!isBurning)
        {
            isBurning = true;

            burnDamage = damage;
            burnTickInterval = Mathf.Max(0.1f, tickInterval);

            nextBurnTickAt = now + burnTickInterval;
            burnExpiresAt = now + duration;
        }
        else
        {
            // Đòn yếu hơn không ghi đè damage mạnh hơn.
            burnDamage = Mathf.Max(burnDamage, damage);

            // Gia hạn mà không lùi lịch tick đang chạy.
            burnExpiresAt = Math.Max(
                burnExpiresAt,
                now + duration
            );
        }
    }

    private void UpdateBurn()
    {
        if (!isBurning || !IsAlive)
            return;

        double now = Time.timeAsDouble;

        // Xử lý tất cả tick đã đến hạn, kể cả tick cuối
        // nếu frame hiện tại chạy muộn hơn thời điểm hết burn.
        while (
            isBurning &&
            IsAlive &&
            nextBurnTickAt <= now + BurnTimeEpsilon &&
            nextBurnTickAt <= burnExpiresAt + BurnTimeEpsilon)
        {
            float damageThisTick = burnDamage;

            // Tăng lịch trước khi gây damage.
            nextBurnTickAt += burnTickInterval;

            TakeDame(damageThisTick);
        }

        if (isBurning && now >= burnExpiresAt)
        {
            ResetBurn();
        }
    }

    private void ResetBurn()
    {
        isBurning = false;

        burnDamage = 0f;
        burnTickInterval = 1f;

        nextBurnTickAt = 0d;
        burnExpiresAt = 0d;
    }
}