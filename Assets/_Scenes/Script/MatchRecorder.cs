using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchRecorder : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private EnemySpawn enemySpawn;

    [Header("Chu kỳ lưu - giây thực")]
    [Min(1f)]
    [SerializeField] private float saveInterval = 30f;

    [Serializable]
    public class MatchData
    {
        public int schemaVersion = 1;

        public string matchId;
        public string sceneName;
        public string startedAtUtc;
        public string endedAtUtc;

        // InProgress, Won, Lost, Left hoặc Quit.
        public string result;

        public List<MatchSample> samples =
            new List<MatchSample>();
    }

    [Serializable]
    public class MatchSample
    {
        public string recordedAtUtc;
        public string reason;

        public int wave;
        public bool waveStarted;
        public bool waveRunning;

        public int hp;
        public int gold;
        public int towerCount;

        // Tổng số quái lọt từ đầu trận.
        public int enemiesLeaked;

        // Thời gian trong game khi đang chiến đấu.
        public float combatSeconds;

        // Thời gian thực kể từ khi vào màn.
        public float elapsedRealSeconds;
    }

    private MatchData matchData;
    private string filePath;

    private double startedAtReal;
    private double nextSaveAtReal;
    private double combatSeconds;

    private int enemiesLeaked;

    private bool initialized;
    private bool finished;

    private void Awake()
    {
        if (enemySpawn == null)
        {
            enemySpawn = FindFirstObjectByType<EnemySpawn>();
        }

        EnemyMove.OnEnemyReachEnd += HandleEnemyReachEnd;
        EnemySpawn.OnAllWavesCompleted += HandleAllWavesCompleted;
    }

    private void Start()
    {
        if (enemySpawn == null || GameManager.Instance == null)
        {
            Debug.LogError(
                "MatchRecorder cần EnemySpawn và GameManager trong scene.",
                this
            );

            enabled = false;
            return;
        }

        string matchId = Guid.NewGuid().ToString("N");

        matchData = new MatchData
        {
            matchId = matchId,
            sceneName = SceneManager.GetActiveScene().name,
            startedAtUtc = DateTime.UtcNow.ToString("O"),
            result = "InProgress"
        };

        string folderPath = System.IO.Path.Combine(
            Application.persistentDataPath,
            "MatchLogs"
        );

        string fileName =
            $"match_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{matchId}.json";

        filePath = System.IO.Path.Combine(
            folderPath,
            fileName
        );

        startedAtReal = Time.realtimeSinceStartupAsDouble;

        nextSaveAtReal =
            startedAtReal + Mathf.Max(1f, saveInterval);

        initialized = true;

        CaptureSample("Start");

        Debug.Log(
            $"File thống kê trận đấu: {filePath}",
            this
        );
    }

    private void LateUpdate()
    {
        if (!initialized || finished)
            return;

        if (enemySpawn != null &&
            enemySpawn.isActiveAndEnabled &&
            enemySpawn.IsWaveRunning &&
            GameManager.Instance != null &&
            GameManager.Instance.Hp > 0)
        {
            combatSeconds += Time.deltaTime;
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.Hp <= 0)
        {
            FinishMatch("Lost");
            return;
        }

        double now = Time.realtimeSinceStartupAsDouble;

        if (now >= nextSaveAtReal)
        {
            CaptureSample("Interval");

            // Nếu ứng dụng bị khựng, chỉ ghi một mẫu hiện tại.
            double interval = Mathf.Max(1f, saveInterval);

            double intervalsPassed =
                Math.Floor(
                    (now - nextSaveAtReal) / interval
                ) + 1d;

            nextSaveAtReal += intervalsPassed * interval;
        }
    }

    private void HandleEnemyReachEnd(EnemyData data)
    {
        if (!initialized || finished)
            return;

        enemiesLeaked++;
    }

    private void HandleAllWavesCompleted()
    {
        if (!initialized || finished)
            return;

        string result =
            GameManager.Instance != null &&
            GameManager.Instance.Hp <= 0
                ? "Lost"
                : "Won";

        FinishMatch(result);
    }

    private void FinishMatch(string result)
    {
        if (!initialized || finished)
            return;

        finished = true;

        matchData.result = result;
        matchData.endedAtUtc = DateTime.UtcNow.ToString("O");

        CaptureSample("End");
    }

    private void CaptureSample(string reason)
    {
        if (!initialized)
            return;

        GameManager gameManager = GameManager.Instance;

        MatchSample previous =
            matchData.samples.Count > 0
                ? matchData.samples[matchData.samples.Count - 1]
                : null;

        MatchSample sample = new MatchSample
        {
            recordedAtUtc = DateTime.UtcNow.ToString("O"),
            reason = reason,

            wave = enemySpawn != null
                ? enemySpawn.CurrentWaveIndex + 1
                : (previous != null ? previous.wave : 0),

            waveStarted =
                enemySpawn != null && enemySpawn.hasStarted,

            waveRunning =
                enemySpawn != null && enemySpawn.IsWaveRunning,

            hp = gameManager != null
                ? gameManager.Hp
                : (previous != null ? previous.hp : 0),

            gold = gameManager != null
                ? gameManager.Gold
                : (previous != null ? previous.gold : 0),

            towerCount = CountTowers(),

            enemiesLeaked = this.enemiesLeaked,

            combatSeconds = (float)this.combatSeconds,

            elapsedRealSeconds = (float)(
                Time.realtimeSinceStartupAsDouble - startedAtReal
            )
        };

        matchData.samples.Add(sample);

        SaveFile();
    }

    private int CountTowers()
    {
        TowerShoot[] towers = FindObjectsByType<TowerShoot>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        return towers.Length;
    }

    private void SaveFile()
    {
        try
        {
            string directoryPath =
                System.IO.Path.GetDirectoryName(filePath);

            Directory.CreateDirectory(directoryPath);

            string json = JsonUtility.ToJson(matchData, true);

            File.WriteAllText(filePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Không lưu được thống kê trận đấu:\n{exception.Message}",
                this
            );
        }
    }

    // Gọi trước khi Restart hoặc về Main Menu.
    public void EndBeforeLeaving()
    {
        if (!initialized)
            return;

        if (!finished)
        {
            string result =
                GameManager.Instance != null &&
                GameManager.Instance.Hp <= 0
                    ? "Lost"
                    : "Left";

            FinishMatch(result);
        }
        else
        {
            SaveFile();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && initialized && !finished)
        {
            CaptureSample("ApplicationPause");
        }
    }

    private void OnApplicationQuit()
    {
        if (!initialized)
            return;

        if (!finished)
        {
            FinishMatch("Quit");
        }
        else
        {
            SaveFile();
        }
    }

    private void OnDestroy()
    {
        EnemyMove.OnEnemyReachEnd -= HandleEnemyReachEnd;
        EnemySpawn.OnAllWavesCompleted -= HandleAllWavesCompleted;

        // Lưu dữ liệu đã thu thập nếu chưa kết thúc trận.
        if (initialized && !finished)
        {
            matchData.result = "Left";
            matchData.endedAtUtc = DateTime.UtcNow.ToString("O");

            SaveFile();
        }
    }
}