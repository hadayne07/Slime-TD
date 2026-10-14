using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    public static Action<int> OnWaveChanged;
    [SerializeField] private Wavedata[] waves;
    [SerializeField] protected int currentWaveIndex = 0;
    private Wavedata CurrentWave => waves[currentWaveIndex];
    [SerializeField] private ObjectPooler Greenpool;
    [SerializeField] private ObjectPooler Bluepool;
    [SerializeField] private ObjectPooler Redpool;
    [SerializeField] private int enemiesRemoved;
    public int EnemisRemoved { get => enemiesRemoved; }
    [SerializeField] private int spawncouter = 0;
    [SerializeField] protected float timespawn = 0;

    

    private int waveCouter = 0;

    private float timeBetweenWaves = 2f;
    [SerializeField] private float wavesCoolDown;
    [SerializeField] private bool isBetweenWaves = false;
   

    private Dictionary<EnemyType, ObjectPooler> poolDictionary;

    private void Awake()
    {

        poolDictionary = new Dictionary<EnemyType, ObjectPooler>()
        {
            { EnemyType.Green , Greenpool},
            { EnemyType.Blue , Bluepool},
            { EnemyType.Red , Redpool}

        };
    }
    private void OnEnable()
    {
        EnemyMove.OnEnemyReachEnd += HandleEnemyReachEnd;
        EnemyStatus.enemyDie += HandleEnemyDie;
    }

    private void OnDisable()
    {
        EnemyMove.OnEnemyReachEnd -= HandleEnemyReachEnd;
        EnemyStatus.enemyDie -= HandleEnemyDie;
    }


   

    private void Update()
    {
        CheckWave();
    

    }


    private void SpawnEnemy()
    {
        if (spawncouter < CurrentWave.enemyPerWave)
        {
            timespawn += Time.deltaTime;
            if (timespawn < CurrentWave.timeSpawn) return;
            timespawn = 0f;
            if (poolDictionary.TryGetValue(CurrentWave.enemy, out var pool))
            {
                GameObject newEnemy = pool.GetPoolObj();
                newEnemy.transform.position = transform.position;

                newEnemy.SetActive(true);
            }
            spawncouter++;
        }
       


    }


    private void Start()
    {
        OnWaveChanged?.Invoke(waveCouter);
    }


    protected void CheckWave()
    {
        if (isBetweenWaves)
        {
            wavesCoolDown -= Time.deltaTime;
          
            if (wavesCoolDown <= 0)
            {
                waveCouter++;
                currentWaveIndex = (currentWaveIndex + 1) % waves.Length;
                spawncouter = 0;
                enemiesRemoved = 0;
                timespawn = 0f;
                OnWaveChanged?.Invoke(waveCouter);
                isBetweenWaves = false;

            }
        }
        else
        {
            if (spawncouter >= CurrentWave.enemyPerWave && enemiesRemoved >= CurrentWave.enemyPerWave)
            {

                wavesCoolDown = timeBetweenWaves;
                isBetweenWaves = true;
            }
            else
            SpawnEnemy();
        }
    }

    protected void HandleEnemyReachEnd(EnemyData data)
    {
        enemiesRemoved++;
    }

    protected void HandleEnemyDie ( EnemyStatus enemy)
    {
        enemiesRemoved++;
    }
}
