using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static Action<int> CurrentHp;
    public static Action<int> CurrentGold;

    [Header("Player")]
    [SerializeField] private int hp = 100;

    [Min(0)]
    [SerializeField] protected int gold = 100;

    [Header("Game Speed")]
    [SerializeField] private float gamespeed = 1f;

    public int Hp => hp;
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
        gold = Mathf.Max(0, gold);

        SetTimeScale(gamespeed);
    }

    private void OnEnable()
    {
        if (Instance != this) return;

        EnemyMove.OnEnemyReachEnd += HandleEnemyReachEnd;
        EnemyStatus.goldplus += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        EnemyMove.OnEnemyReachEnd -= HandleEnemyReachEnd;
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

    private void HandleEnemyReachEnd(EnemyData data)
    {
        if (data == null) return;

        hp = Mathf.Max(0, hp - data.Damage);

        CurrentHp?.Invoke(hp);
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