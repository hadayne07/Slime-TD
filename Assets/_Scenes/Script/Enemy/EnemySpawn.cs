using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    public static Action OnAllWavesCompleted;
    public static Action<int> OnWaveChanged;

    [Header("Wave")]
    [SerializeField] private Wavedata[] waves;
    [SerializeField] protected int currentWaveIndex =0;

    public int CurrentWaveIndex => currentWaveIndex;
    private Wavedata CurrentWave => waves[currentWaveIndex];

    [Header("Pool")]
    [SerializeField] private ObjectPooler Greenpool;
    [SerializeField] private ObjectPooler Bluepool;
    [SerializeField] private ObjectPooler Redpool;
    [SerializeField] private ObjectPooler Bosspool;

    [Header("Theo dõi")]
    [SerializeField] private int enemiesRemoved;
    [SerializeField] private int spawncouter;
    [SerializeField] private int groupsStillSpawning;
    [SerializeField] private float wavesCoolDown;
    [SerializeField] private bool isBetweenWaves;

    public int EnemisRemoved => enemiesRemoved;

    public bool hasStarted;
    private bool allWavesCompleted;
    private bool hasSpawnError;

    private Dictionary<EnemyType, ObjectPooler> poolDictionary;

    private void Awake()
    {
        poolDictionary = new Dictionary<EnemyType, ObjectPooler>
        {
            { EnemyType.Green, Greenpool },
            { EnemyType.Blue, Bluepool },
            { EnemyType.Red, Redpool },
            {EnemyType.Boss , Bosspool }
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

    private void Start()
    {
        if (!ValidateWaves())
        {
            enabled = false;
            return;
        }

    }

    private void Update()
    {
        if (!hasStarted || allWavesCompleted || hasSpawnError)
            return;
          CheckWave(); 
    }
    public void TriggerStartWave()
    {

        if (!hasStarted)
        {
            hasStarted = true;
            BeginCurrentWave();
            return;
        }
        if (hasStarted && isBetweenWaves)
        {
            currentWaveIndex++;
            if (currentWaveIndex >= waves.Length)
            {
                CompleteAllWaves();
                return;
            }

            BeginCurrentWave();
        }
    }
    private bool ValidateWaves()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("EnemySpawn chưa được gán Wave.", this);
            return false;
        }

        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Length)
        {
            Debug.LogError("Current Wave Index không hợp lệ.", this);
            return false;
        }

        for (int i = currentWaveIndex; i < waves.Length; i++)
        {
            Wavedata wave = waves[i];

            if (wave == null ||
                wave.groups == null ||
                wave.groups.Length == 0)
            {
                Debug.LogError(
                    $"Wave {i + 1} chưa có nhóm quái.",
                    this
                );
                return false;
            }

            foreach (EnemyGroup group in wave.groups)
            {
                if (group == null || group.count <= 0)
                {
                    Debug.LogError(
                        $"Wave {i + 1} có nhóm quái không hợp lệ.",
                        this
                    );
                    return false;
                }

                if (!poolDictionary.TryGetValue(
                        group.enemyType, out ObjectPooler pool) ||
                    pool == null)
                {
                    Debug.LogError(
                        $"Wave {i + 1}: chưa gán pool {group.enemyType}.",
                        this
                    );
                    return false;
                }
            }
        }

        return true;
    }

    private void BeginCurrentWave()
    {
        spawncouter = 0;
        enemiesRemoved = 0;
        groupsStillSpawning = 0;
        wavesCoolDown = 0f;
        isBetweenWaves = false;

        OnWaveChanged?.Invoke(currentWaveIndex);
        StartWaveGroup(CurrentWave);
    }

    protected void CheckWave()
    {
        if (isBetweenWaves)
        {
            wavesCoolDown -= Time.deltaTime;

            if (wavesCoolDown > 0f)
                return;
            currentWaveIndex++;
            if (currentWaveIndex >= waves.Length)
            {
                CompleteAllWaves();
                return;
            }
            BeginCurrentWave();
            return;
        }

        if (groupsStillSpawning > 0 || enemiesRemoved < spawncouter)
            return;

        if (currentWaveIndex >= waves.Length - 1)
        {
            CompleteAllWaves();
            return;
        }

        wavesCoolDown = Mathf.Max(0f, CurrentWave.TimeNextWave);
        isBetweenWaves = true;
    }
    private void CompleteAllWaves()
    {
        if (allWavesCompleted)
            return;

        allWavesCompleted = true;
        enabled = false;

        OnAllWavesCompleted?.Invoke();
    }

    protected void HandleEnemyReachEnd(EnemyData data)
    {
        if (!hasStarted || allWavesCompleted)
            return;

        enemiesRemoved++;
    }

    protected void HandleEnemyDie(EnemyStatus enemy)
    {
        if (!hasStarted || allWavesCompleted)
            return;

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
            if (hasSpawnError)
                break;

            if (!SpawnOneEnemy(group.enemyType))
            {
                // Không tự chuyển Wave nếu spawn bị lỗi.
                hasSpawnError = true;

                Debug.LogError(
                    "Đã dừng spawn. Hãy sửa lỗi trong Console rồi chạy lại.",
                    this
                );
                break;
            }

            if (i < group.count - 1)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0.01f, group.spawnInterval)
                );
            }
        }

        groupsStillSpawning--;
    }

    private bool SpawnOneEnemy(EnemyType enemyType)
    {
        if (!poolDictionary.TryGetValue(
                enemyType, out ObjectPooler pool) ||
            pool == null)
        {
            Debug.LogError($"Chưa gán pool {enemyType}.", this);
            return false;
        }

        GameObject enemy = pool.GetPoolObj();

        if (enemy == null)
        {
            Debug.LogError($"Pool {enemyType} không trả về enemy.", this);
            return false;
        }

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();

        if (status == null)
        {
            Debug.LogError("Prefab enemy thiếu EnemyStatus.", enemy);
            return false;
        }

        // Chuẩn bị HP trước khi bật quái.
        status.PrepareForWave(currentWaveIndex + 1);

        // Mọi quái xuất hiện tại vị trí EnemySpawn.
        enemy.transform.position = transform.position;

        spawncouter++;
        enemy.SetActive(true);

        if (!enemy.activeInHierarchy)
        {
            spawncouter--;

            Debug.LogError(
                "Enemy không được kích hoạt. Kiểm tra EnemyData và pool.",
                enemy
            );
            return false;
        }

        return true;
    }
}