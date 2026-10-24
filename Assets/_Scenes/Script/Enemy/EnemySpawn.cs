
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class EnemySpawn : MonoBehaviour
{
    public static Action OnAllWavesCompleted;
    public int CurrentWaveIndex => currentWaveIndex;
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

    [SerializeField] private int groupsStillSpawning;
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

    private void Start()
    {
        OnWaveChanged?.Invoke(currentWaveIndex);
        StartWaveGroup(CurrentWave);
    }

    protected void CheckWave()
    {
        if (isBetweenWaves)
        {
            wavesCoolDown -= Time.deltaTime;

            if (wavesCoolDown <= 0)
            {
                if (currentWaveIndex + 1 >= waves.Length)
                {
                    // Đã hết wave — gọi OnAllWavesCompleted trước khi tắt
                    enabled = false;
                    OnAllWavesCompleted?.Invoke();
                    return;
                }
                currentWaveIndex++;
                spawncouter = 0;
                enemiesRemoved = 0;
                OnWaveChanged?.Invoke(currentWaveIndex);
                isBetweenWaves = false;
                StartWaveGroup(CurrentWave);
            }
        }
        else
        {
            if (groupsStillSpawning == 0 && enemiesRemoved >= spawncouter)
            {
                if (currentWaveIndex >= waves.Length - 1)
                {
                    enabled = false;
                    OnAllWavesCompleted?.Invoke();
                    return;
                }

                wavesCoolDown = CurrentWave.TimeNextWave;
                isBetweenWaves = true;
            }
        }
    }

    protected void HandleEnemyReachEnd(EnemyData data)
    {
        enemiesRemoved++;
    }

    protected void HandleEnemyDie(EnemyStatus enemy)
    {
        enemiesRemoved++;
    }

    private void StartWaveGroup(Wavedata wave)
    {
        groupsStillSpawning = wave.groups.Length;
        foreach (EnemyGroup group in wave.groups)
        {
            StartCoroutine(SpawnGroup(group));
        }
    }

    private IEnumerator SpawnGroup(EnemyGroup group)
    {
        if (group.startDelay > 0f)
        {
            yield return new WaitForSeconds(group.startDelay);
        }

        for (int i = 0; i < group.count; i++)
        {
            SpawnOneEnemy(group.enemyType);
            if (i < group.count - 1)
            {
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }
        groupsStillSpawning--;
    }

    private void SpawnOneEnemy(EnemyType enemyType)
    {
        ObjectPooler pool = poolDictionary[enemyType];
        GameObject enemy = pool.GetPoolObj();
        if (enemy == null) return;
        enemy.transform.position = transform.position;
        spawncouter++;
        enemy.SetActive(true);
    }
}
