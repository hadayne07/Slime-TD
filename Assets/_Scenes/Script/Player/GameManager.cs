using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static Action<int> CurrentHp;
    public static Action<int> CurrentGold;
    public static Action<int, int> OnPlayerHit;

    [Header("Player")]
    [SerializeField] private int hp = 100;

    [Min(0)]
    [SerializeField] protected int gold = 100;

    [Header("Game Speed")]
    [SerializeField] private float gamespeed = 1f;

    public int Hp => hp;
    // HP khởi đầu được dùng làm HP tối đa trong prototype hiện tại (chưa có hồi máu).
    public int StartingHp { get; private set; }
    public int Gold => gold;
    public float GameSpeed => gamespeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        hp = Mathf.Max(0, hp);
        StartingHp = hp;
        gold = Mathf.Max(0, gold);

        SetTimeScale(gamespeed);
    }

    private void OnEnable()
    {
        if (Instance != this) return;

        EnemyMove.OnEnemyLeaked += HandleEnemyLeaked;
        EnemyStatus.goldplus += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        EnemyMove.OnEnemyLeaked -= HandleEnemyLeaked;
        EnemyStatus.goldplus -= HandleEnemyDestroyed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        CurrentHp?.Invoke(hp);
        CurrentGold?.Invoke(gold);
    }

    private void HandleEnemyLeaked(EnemyData data, EnemyType enemyType)
    {
        if (data == null || hp <= 0) return;

        int hpBefore = hp;
        hp = Mathf.Max(0, hp - data.Damage);

        TelemetryEventLogger.RecordEnemyLeak(enemyType, data.Damage, hpBefore - hp, hp);
        OnPlayerHit?.Invoke(data.Damage, hp);
        CurrentHp?.Invoke(hp);

        if (hp <= 0)
        {
            TelemetryEventLogger.RecordDeath();
        }
    }

    private void HandleEnemyDestroyed(EnemyData data)
    {
        if (data == null) return;

        AddGold(data.GoldFall);
    }

    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            return;
        }

        gold += amount;

        CurrentGold?.Invoke(gold);
    }

    public bool TrySpendGold(int cost)
    {
        if (cost < 0)
        {
            return false;
        }

        if (gold < cost)
        {
            return false;
        }

        gold -= cost;

        CurrentGold?.Invoke(gold);

        return true;
    }

    public void SpendGold(int cost)
    {
        TrySpendGold(cost);
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = Mathf.Max(0f, scale);
    }

    public void SetGameSpeed(float newSpeed)
    {
        gamespeed = Mathf.Max(0f, newSpeed);

        SetTimeScale(gamespeed);
    }
}
