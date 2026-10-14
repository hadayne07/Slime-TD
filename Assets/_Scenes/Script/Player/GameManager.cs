    using System;
    using UnityEngine;

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int hp = 100;
        public int Hp { get => hp; }
        public static Action<int> CurrentHp;
        public static Action<int> CurrentGold;
    [SerializeField] protected int gold = 0;
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
    }
