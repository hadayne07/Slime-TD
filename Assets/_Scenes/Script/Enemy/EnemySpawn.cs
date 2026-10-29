using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    public static Action OnAllWavesCompleted;
    public static Action<int> OnWaveChanged;

    [Header("Wave")]
    [SerializeField] private Wavedata[] waves;
    [SerializeField] private int currentWaveIndex;

    public int CurrentWaveIndex => currentWaveIndex;

    [Header("Pool")]
    [SerializeField] private ObjectPooler Greenpool;
    [SerializeField] private ObjectPooler Bluepool;
    [SerializeField] private ObjectPooler Redpool;
    [SerializeField] private ObjectPooler Bosspool;

    [Header("Theo dõi")]
    [SerializeField] private int enemiesRemoved;
    [SerializeField] private int spawncouter;
    [SerializeField] private int groupsStillSpawning;

    public int EnemisRemoved => enemiesRemoved;

    // Giữ tên để tương thích với UI cũ.
    public bool hasStarted { get; private set; }

    private bool initialized;
    private bool waveRunning;
    private bool allWavesCompleted;
    private bool hasSpawnError;

    private Dictionary<EnemyType, ObjectPooler> poolDictionary;

    // Tiến độ riêng của từng nhóm trong Wave.
    private class GroupProgress
    {
        public EnemyType enemyType;
        public int remaining;
        public float interval;
        public float timeUntilSpawn;
    }

    private readonly List<GroupProgress> activeGroups =
        new List<GroupProgress>();
    public bool IsWaveRunning => waveRunning;
    public bool CanStartNextWave => initialized &&
        isActiveAndEnabled &&
        !waveRunning &&
        !allWavesCompleted &&
        !hasSpawnError &&
        GameManager.Instance != null &&
        GameManager.Instance.Hp > 0 &&
        Time.timeScale > 0f &&
        (UIController.Instance == null ||
         !UIController.Instance.IsGameplayBlocked);

    private void Awake()
    {
        poolDictionary = new Dictionary<EnemyType, ObjectPooler>
        {
            { EnemyType.Green, Greenpool },
            { EnemyType.Blue, Bluepool },
            { EnemyType.Red, Redpool },
            { EnemyType.Boss, Bosspool }
        };

        // Giữ đăng ký trong suốt vòng đời object:
        // spawner tạm tắt vẫn đếm được quái đã chết/tới đích.
        EnemyMove.OnEnemyReachEnd += HandleEnemyReachEnd;
        EnemyStatus.enemyDie += HandleEnemyDie;
    }

    private void OnDestroy()
    {
        EnemyMove.OnEnemyReachEnd -= HandleEnemyReachEnd;
        EnemyStatus.enemyDie -= HandleEnemyDie;
    }

    private void Start()
    {
        if (waves == null ||
            waves.Length == 0 ||
            currentWaveIndex < 0 ||
            currentWaveIndex >= waves.Length)
        {
            SetSpawnError("Danh sách Wave hoặc chỉ số Wave không hợp lệ.");
            return;
        }

        initialized = true;

        // Chỉ hiển thị, chưa sinh quái.
        OnWaveChanged?.Invoke(currentWaveIndex);
    }

    public void TriggerStartWave()
    {
        if (!CanStartNextWave)
            return;

        int nextIndex = hasStarted
            ? currentWaveIndex + 1
            : currentWaveIndex;

        if (nextIndex >= waves.Length)
            return;

        if (!ValidateWave(waves[nextIndex]))
            return;

        currentWaveIndex = nextIndex;
        BeginCurrentWave();
    }

    private bool ValidateWave(Wavedata wave)
    {
        if (wave == null ||
            wave.groups == null ||
            wave.groups.Length == 0)
        {
            SetSpawnError("Wave chưa có nhóm quái.");
            return false;
        }

        foreach (EnemyGroup group in wave.groups)
        {
            if (group == null || group.count <= 0)
            {
                SetSpawnError("Wave có nhóm quái không hợp lệ.");
                return false;
            }

            if (!poolDictionary.TryGetValue(
                    group.enemyType, out ObjectPooler pool) ||
                pool == null)
            {
                SetSpawnError($"Chưa gán pool {group.enemyType}.");
                return false;
            }
        }

        return true;
    }

    private void BeginCurrentWave()
    {
        hasStarted = true;
        waveRunning = true;

        spawncouter = 0;
        enemiesRemoved = 0;

        activeGroups.Clear();

        foreach (EnemyGroup group in waves[currentWaveIndex].groups)
        {
            activeGroups.Add(new GroupProgress
            {
                enemyType = group.enemyType,
                remaining = group.count,
                interval = Mathf.Max(0.01f, group.spawnInterval),
                timeUntilSpawn = Mathf.Max(0f, group.startDelay)
            });
        }

        groupsStillSpawning = activeGroups.Count;

        OnWaveChanged?.Invoke(currentWaveIndex);
    }

    private void Update()
    {
        if (!initialized ||
            !waveRunning ||
            allWavesCompleted ||
            hasSpawnError)
        {
            return;
        }

        if (Time.deltaTime <= 0f)
            return;

        if (GameManager.Instance == null ||
            GameManager.Instance.Hp <= 0)
        {
            return;
        }

        if (UIController.Instance != null &&
            UIController.Instance.IsGameplayBlocked)
        {
            return;
        }

        foreach (GroupProgress group in activeGroups)
        {
            if (group.remaining <= 0)
                continue;

            group.timeUntilSpawn -= Time.deltaTime;

            // Giữ phần thời gian dư nếu frame chậm.
            while (group.remaining > 0 &&
                   group.timeUntilSpawn <= 0f)
            {
                if (!SpawnOneEnemy(group.enemyType))
                {
                    SetSpawnError(
                        "Spawn thất bại. Kiểm tra prefab, EnemyData và pool."
                    );
                    return;
                }

                group.remaining--;

                if (group.remaining == 0)
                {
                    groupsStillSpawning--;
                }
                else
                {
                    group.timeUntilSpawn += group.interval;
                }
            }
        }

        CheckWaveFinished();
    }

    private void CheckWaveFinished()
    {
        if (groupsStillSpawning > 0 ||
            enemiesRemoved < spawncouter)
        {
            return;
        }

        waveRunning = false;
        activeGroups.Clear();

        if (currentWaveIndex >= waves.Length - 1)
        {
            allWavesCompleted = true;
            OnAllWavesCompleted?.Invoke();
        }

        // Nếu chưa phải Wave cuối:
        // giữ trạng thái chờ vô hạn cho tới khi bấm nút.
    }

    private void HandleEnemyReachEnd(EnemyData data)
    {
        if (waveRunning && !allWavesCompleted)
        {
            enemiesRemoved++;
        }
    }

    private void HandleEnemyDie(EnemyStatus enemy)
    {
        if (waveRunning && !allWavesCompleted)
        {
            enemiesRemoved++;
        }
    }

    private bool SpawnOneEnemy(EnemyType enemyType)
    {
        if (!poolDictionary.TryGetValue(
                enemyType, out ObjectPooler pool) ||
            pool == null)
        {
            return false;
        }

        GameObject enemy = pool.GetPoolObj();

        if (enemy == null)
            return false;

        EnemyStatus status = enemy.GetComponent<EnemyStatus>();

        if (status == null)
        {
            Debug.LogError("Prefab thiếu EnemyStatus.", enemy);
            return false;
        }

        status.PrepareForWave(currentWaveIndex + 1);
        enemy.transform.position = transform.position;

        spawncouter++;
        enemy.SetActive(true);

        if (!enemy.activeInHierarchy)
        {
            spawncouter--;
            return false;
        }

        return true;
    }

    private void SetSpawnError(string message)
    {
        hasSpawnError = true;
        Debug.LogError(message, this);
    }
}