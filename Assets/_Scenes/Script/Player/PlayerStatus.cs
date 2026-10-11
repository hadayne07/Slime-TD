    using System;
    using UnityEngine;

    public class PlayerStatus : MonoBehaviour
    {
        [SerializeField] private int hp = 100;
        public int Hp { get => hp; }
        public static Action<int> CurrentHp;
    private void OnEnable()
    {
        EnemyMove.OnEnemyReachEnd += HandleEnemyReachEnd;
    }

    private void OnDisable()
    {
        EnemyMove.OnEnemyReachEnd -= HandleEnemyReachEnd;
    }

    private void Start()
    {
        CurrentHp?.Invoke(hp);
    }




    protected void HandleEnemyReachEnd( EnemyData data)
        {
        hp =  Mathf.Max(0, hp -= data.Damage);
            CurrentHp?.Invoke(hp);
        }
      
    }
