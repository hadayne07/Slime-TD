using System;
using System.Collections;
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

    public bool IsAlive =>
        !isDead && hp > 0f && isActiveAndEnabled;

    // Slow theo phần trăm.
    private const float MaxSlowPercent = 0.4f;
    private float activeSlowPercent;
    private float slowExpiresAt;

    // Burn.
    private Coroutine burnCoroutine;
    private float burnExpiresAt;
    private float burnTickInterval = 1f;
    private float burnDamage;

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

        burnCoroutine = null;
        burnExpiresAt = 0f;
        burnTickInterval = 1f;
        burnDamage = 0f;

        UpdateHealthBar();
    }

    private void Update()
    {
        if (!IsAlive)
            return;

        UpdateSlow();
    }

    private void OnDisable()
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }

        burnCoroutine = null;
        burnExpiresAt = 0f;
        burnDamage = 0f;

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
            1f + Mathf.Max(0, waveNumber - 10) * 0.1f;
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

        burnDamage = damage;
        burnTickInterval = Mathf.Max(0.1f, tickInterval);
        burnExpiresAt = Time.time + duration;

        if (burnCoroutine == null)
        {
            burnCoroutine = StartCoroutine(BurnRoutine());
        }
    }

    private IEnumerator BurnRoutine()
    {
        while (IsAlive && Time.time < burnExpiresAt)
        {
            yield return new WaitForSeconds(burnTickInterval);

            if (!IsAlive)
                break;

            if (Time.time <= burnExpiresAt)
            {
                TakeDame(burnDamage);
            }
        }

        burnCoroutine = null;
        burnDamage = 0f;
    }
}