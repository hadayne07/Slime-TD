    using System;
    using UnityEngine;

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int hp = 100;
    [SerializeField] private float gamespeed = 1f;
    public float GameSpeed => gamespeed;
        public int Hp { get => hp; }
        public static Action<int> CurrentHp;
        public static Action<int> CurrentGold;
        public static GameManager Instance { get; private set; }
    [SerializeField] protected int gold;
    public int Gold => gold;

    private void Awake()
    {
        if ( Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void OnEnable()
    {
        EnemyMove.OnEnemyReachEnd += HandleEnemyReachEnd;
        EnemyStatus.goldplus += HandleEnemyDestroyed;
  


    }

    private void OnDisable()
    {
        EnemyMove.OnEnemyReachEnd -= HandleEnemyReachEnd;
        EnemyStatus.goldplus -= HandleEnemyDestroyed;

    }

    private void Start()
    {
        CurrentHp?.Invoke(hp);
        CurrentGold?.Invoke(gold);
    }




    protected void HandleEnemyReachEnd( EnemyData data)
        {
        hp =  Mathf.Max(0, hp -= data.Damage);
            CurrentHp?.Invoke(hp);
        }
     
    protected void UpdateGold( int  resource)
    {
        gold += resource;
   
    }

     protected void HandleEnemyDestroyed(EnemyData data)
    {
        UpdateGold(data.GoldFall);
        CurrentGold?.Invoke(gold);

    }

    

    public void SetTimeScale ( float scale)
    {
        Time.timeScale = scale;
    }

    public void SetGameSpeed( float newspeed)
    {
        gamespeed = newspeed;
        SetTimeScale(gamespeed);
    }
    
    public void SpendGold( int cost)
    {
        gold -= cost;
        CurrentGold?.Invoke(gold);
    }
    }
