using System;
using System.Collections;
using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    public static Action<EnemyStatus> enemyDie;
    public static Action<EnemyData> goldplus;
    [SerializeField] private float hp;

    [SerializeField] private Transform healthbar;
    private Vector3 healthBarOriginalScale;

    private Coroutine burnCoroutine;
    private float burnExpiresAt;
    private float burnTickInterval = 1f;
    private float burnDamage;

    public float currentSpeed;
    private bool isSlowed = false;

    private void OnEnable()
    {
        hp = data.Hp;
        currentSpeed = data.Speed;
        burnCoroutine = null;
        burnExpiresAt = 0f;
        burnTickInterval = 1f;
        burnDamage = 0;
        UpdateHealthBar();
    }

    private void Awake()
    {
        healthBarOriginalScale = healthbar.localScale;
    }

    private void OnDisable()
    {
        if (burnCoroutine != null)
            StopCoroutine(burnCoroutine);

        burnCoroutine = null;
        burnExpiresAt = 0f;
        burnDamage = 0;
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

    public void TakeDame(float dame)
    {
        hp -= dame;
        UpdateHealthBar();
    }

    protected void UpdateHealthBar()
    {
        float healthBarPercent = hp / data.Hp;
        Vector3 scale = healthBarOriginalScale;
        scale.x = healthBarOriginalScale.x * healthBarPercent;
        healthbar.localScale = scale;
    }

    public void HasSlowed(float slow)
    {
        currentSpeed -= slow;
    }

    public void SetSpeed(float slow)
    {
       currentSpeed+= slow;
    }


    public void ApplyBurn(float damage, float duration, float tickInterval = 1f)
    {
        if (hp <= 0 || damage <= 0 || duration <= 0 || !isActiveAndEnabled)
            return;

        burnDamage = damage;
        burnTickInterval = Mathf.Max(0.1f, tickInterval);
        burnExpiresAt = Time.time + duration;

        if (burnCoroutine == null)
            burnCoroutine = StartCoroutine(BurnRoutine());
    }

    private IEnumerator BurnRoutine()
    {
        while (hp > 0 && Time.time < burnExpiresAt)
        {
            yield return new WaitForSeconds(burnTickInterval);

            if (hp <= 0 || !isActiveAndEnabled)
                break;

   
            if (Time.time <= burnExpiresAt)
                TakeDame(burnDamage);
        }

        burnCoroutine = null;
        burnDamage = 0;
    }

    public void ApplySlowEffect(float slowAmount, float duration)
    {
        if (!isSlowed)
            StartCoroutine(SlowRoutine(slowAmount, duration));
    }

    private IEnumerator SlowRoutine(float slowAmount, float duration)
    {
        isSlowed = true;
        HasSlowed(slowAmount);


        float timer = 0f;
        while (timer < duration && hp > 0)
        {
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }

        if (hp > 0)
        {
            SetSpeed(slowAmount);

        }

        isSlowed = false;
    }
}
