using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchRecorder : MonoBehaviour
{
    private const string SnapshotSchemaVersion = "telemetry-v1";
    private const string ObjectiveId = "defend_core";
    private const double RecentWindowSeconds = 5d;
    private const double LongWindowSeconds = 30d;

    [Header("Tham chiếu")]
    [SerializeField] private EnemySpawn enemySpawn;

    [Header("Chu kỳ lưu snapshot - giây thực")]
    [Min(1f)]
    [SerializeField] private float saveInterval = 5f;

    [Serializable]
    public class MatchData
    {
        public int schemaVersion = 2;
        public string telemetrySchemaVersion = SnapshotSchemaVersion;
        public string sessionId;
        public string playerIdHash;
        public string runId;
        public string matchId;
        public string sceneName;
        public string mapId;
        public string patchId;
        public string clientBuild;
        public string deviceClass;
        public string startedAtUtc;
        public string endedAtUtc;

        // InProgress, Won, Lost, Left hoặc Quit.
        public string result;

        public List<MatchSample> samples = new List<MatchSample>();
    }

    [Serializable]
    public class MatchSample
    {
        public string schemaVersion;
        public string sessionId;
        public string playerIdHash;
        public string runId;
        public string matchId;
        public string mapId;
        public string patchId;
        public string recordedAtUtc;
        public float elapsedRealSeconds;
        public string segmentId;
        public string objectiveId;
        public string reason;

        public int wave;
        public bool hasWave;
        public bool waveStarted;
        public bool waveRunning;

        // Giữ has* làm mask tương thích pipeline; trường thiếu được serialize thành JSON null.
        public int currentDifficulty;
        public bool hasCurrentDifficulty;
        public float progressRatio;
        public bool hasProgressRatio;
        public int hp;
        public bool hasHp;
        public int hpMax;
        public bool hasHpMax;
        public int gold;
        public bool hasGold;
        public int towerCount;
        public bool hasTowerCount;
        public int enemyAlive;
        public bool hasEnemyAlive;
        public int kills5s;
        public bool hasKills5s;
        public int hitsTaken5s;
        public bool hasHitsTaken5s;
        public int hitsTaken30s;
        public bool hasHitsTaken30s;
        public int deaths30s;
        public bool hasDeaths30s;
        public int enemyLeaks30s;
        public bool hasEnemyLeaks30s;
        public int actionCount5s;
        public bool hasActionCount5s;
        public float invalidActionRate;
        public bool hasInvalidActionRate;
        public float idleRatio5s;
        public bool hasIdleRatio5s;
        public float scoreEma;
        public bool hasScoreEma;

        // Tương thích với logger trận cũ; combatSeconds là thời gian game, không phải elapsed thực.
        public int enemiesLeaked;
        public float combatSeconds;
    }

    private sealed class TimedAction
    {
        public double timestamp;
        public bool invalid;
    }

    private MatchData matchData;
    private string filePath;
    private string snapshotFilePath;

    private double startedAtReal;
    private double nextSaveAtReal;
    private double combatSeconds;
    private int enemiesLeaked;
    private bool initialized;
    private bool finished;

    private readonly Queue<double> enemyKillTimes = new Queue<double>();
    private readonly Queue<double> playerHitTimes = new Queue<double>();
    private readonly Queue<double> playerDeathTimes = new Queue<double>();
    private readonly Queue<double> enemyLeakTimes = new Queue<double>();
    private readonly Queue<TimedAction> userActions = new Queue<TimedAction>();

    private void Awake()
    {
        if (enemySpawn == null)
            enemySpawn = FindFirstObjectByType<EnemySpawn>();
    }

    private void OnEnable()
    {
        EnemyMove.OnEnemyReachEnd += HandleEnemyReachEnd;
        EnemySpawn.OnAllWavesCompleted += HandleAllWavesCompleted;
        EnemyStatus.enemyDie += HandleEnemyDie;
        GameManager.OnPlayerHit += HandlePlayerHit;
        TelemetryEventLogger.UserActionRecorded += HandleUserAction;
    }

    private void Start()
    {
        if (enemySpawn == null || GameManager.Instance == null)
        {
            Debug.LogError("MatchRecorder cần EnemySpawn và GameManager trong scene.", this);
            enabled = false;
            return;
        }

        string matchId = TelemetryEventLogger.SessionId;
        if (string.IsNullOrEmpty(matchId))
            matchId = Guid.NewGuid().ToString("N");

        string mapId = SceneManager.GetActiveScene().name;
        matchData = new MatchData
        {
            sessionId = matchId,
            playerIdHash = null,
            runId = matchId,
            matchId = matchId,
            sceneName = mapId,
            mapId = mapId,
            patchId = null,
            clientBuild = Application.version,
            deviceClass = SystemInfo.deviceType.ToString().ToLowerInvariant(),
            startedAtUtc = DateTime.UtcNow.ToString("O"),
            result = "InProgress"
        };

        string matchDirectory = Path.Combine(Application.persistentDataPath, "MatchLogs");
        string telemetryDirectory = Path.Combine(Application.persistentDataPath, "Telemetry");
        string fileName = string.Format("match_{0:yyyyMMdd_HHmmss}_{1}.json", DateTime.UtcNow, matchId);
        string snapshotFileName = string.Format("snapshots_{0:yyyyMMdd_HHmmss}_{1}.jsonl", DateTime.UtcNow, matchId);
        filePath = Path.Combine(matchDirectory, fileName);
        snapshotFilePath = Path.Combine(telemetryDirectory, snapshotFileName);

        startedAtReal = Time.realtimeSinceStartupAsDouble;
        saveInterval = Mathf.Max(1f, saveInterval);
        nextSaveAtReal = startedAtReal + saveInterval;
        initialized = true;

        CaptureSample("Start");
        Debug.Log("File thống kê trận đấu: " + filePath, this);
        Debug.Log("File snapshot JSONL: " + snapshotFilePath, this);
    }

    private void LateUpdate()
    {
        if (!initialized || finished)
            return;

        if (enemySpawn != null && enemySpawn.isActiveAndEnabled && enemySpawn.IsWaveRunning &&
            GameManager.Instance != null && GameManager.Instance.Hp > 0)
        {
            combatSeconds += Time.deltaTime;
        }

        if (GameManager.Instance != null && GameManager.Instance.Hp <= 0)
        {
            FinishMatch("Lost");
            return;
        }

        double now = Time.realtimeSinceStartupAsDouble;
        if (now >= nextSaveAtReal)
        {
            CaptureSample("Interval");
            double interval = Mathf.Max(1f, saveInterval);
            double intervalsPassed = Math.Floor((now - nextSaveAtReal) / interval) + 1d;
            nextSaveAtReal += intervalsPassed * interval;
        }
    }

    private void HandleEnemyReachEnd(EnemyData data)
    {
        if (!initialized || finished)
            return;

        enemiesLeaked++;
        enemyLeakTimes.Enqueue(Time.realtimeSinceStartupAsDouble);
    }

    private void HandleEnemyDie(EnemyStatus enemy)
    {
        if (!initialized || finished)
            return;

        enemyKillTimes.Enqueue(Time.realtimeSinceStartupAsDouble);
    }

    private void HandlePlayerHit(int damage, int hpAfter)
    {
        if (!initialized || finished)
            return;

        double now = Time.realtimeSinceStartupAsDouble;
        playerHitTimes.Enqueue(now);
        if (hpAfter <= 0)
            playerDeathTimes.Enqueue(now);
    }

    private void HandleUserAction(bool invalid)
    {
        if (!initialized || finished)
            return;

        userActions.Enqueue(new TimedAction
        {
            timestamp = Time.realtimeSinceStartupAsDouble,
            invalid = invalid
        });
    }

    private void HandleAllWavesCompleted()
    {
        if (!initialized || finished)
            return;

        string result = GameManager.Instance != null && GameManager.Instance.Hp <= 0
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
        double now = Time.realtimeSinceStartupAsDouble;
        PruneOlderThan(enemyKillTimes, now - LongWindowSeconds);
        PruneOlderThan(playerHitTimes, now - LongWindowSeconds);
        PruneOlderThan(playerDeathTimes, now - LongWindowSeconds);
        PruneOlderThan(enemyLeakTimes, now - LongWindowSeconds);
        PruneActions(now - RecentWindowSeconds);

        bool hasWave = enemySpawn != null && enemySpawn.hasStarted;
        int wave = hasWave ? enemySpawn.CurrentWaveIndex + 1 : 0;
        bool hasProgress = enemySpawn != null && enemySpawn.TotalWaves > 0;
        int completedWaves = 0;
        if (hasWave)
            completedWaves = enemySpawn.CurrentWaveIndex + (enemySpawn.IsWaveRunning ? 0 : 1);
        float progressRatio = hasProgress
            ? Mathf.Clamp01((float)completedWaves / enemySpawn.TotalWaves)
            : 0f;

        bool hasHp = gameManager != null;
        bool hasGold = gameManager != null;
        bool hasHpMax = gameManager != null;
        int activeEnemies = CountLivingEnemies();
        int towerCount = CountTowers();
        int actionCount = userActions.Count;
        int invalidActions = CountInvalidActions();

        MatchSample sample = new MatchSample
        {
            schemaVersion = SnapshotSchemaVersion,
            sessionId = matchData.sessionId,
            playerIdHash = null,
            runId = matchData.runId,
            matchId = matchData.matchId,
            mapId = matchData.mapId,
            patchId = null,
            recordedAtUtc = DateTime.UtcNow.ToString("O"),
            elapsedRealSeconds = (float)(now - startedAtReal),
            segmentId = hasWave ? string.Format("wave-{0:00}", wave) : null,
            objectiveId = ObjectiveId,
            reason = reason,
            wave = wave,
            hasWave = hasWave,
            waveStarted = enemySpawn != null && enemySpawn.hasStarted,
            waveRunning = enemySpawn != null && enemySpawn.IsWaveRunning,
            currentDifficulty = 0,
            hasCurrentDifficulty = false,
            progressRatio = progressRatio,
            hasProgressRatio = hasProgress,
            hp = hasHp ? gameManager.Hp : 0,
            hasHp = hasHp,
            hpMax = hasHpMax ? gameManager.StartingHp : 0,
            hasHpMax = hasHpMax,
            gold = hasGold ? gameManager.Gold : 0,
            hasGold = hasGold,
            towerCount = towerCount,
            hasTowerCount = true,
            enemyAlive = activeEnemies,
            hasEnemyAlive = true,
            kills5s = CountSince(enemyKillTimes, now - RecentWindowSeconds),
            hasKills5s = true,
            hitsTaken5s = CountSince(playerHitTimes, now - RecentWindowSeconds),
            hasHitsTaken5s = true,
            hitsTaken30s = CountSince(playerHitTimes, now - LongWindowSeconds),
            hasHitsTaken30s = true,
            deaths30s = CountSince(playerDeathTimes, now - LongWindowSeconds),
            hasDeaths30s = true,
            enemyLeaks30s = CountSince(enemyLeakTimes, now - LongWindowSeconds),
            hasEnemyLeaks30s = true,
            actionCount5s = actionCount,
            hasActionCount5s = true,
            invalidActionRate = actionCount > 0 ? (float)invalidActions / actionCount : 0f,
            hasInvalidActionRate = actionCount > 0,
            idleRatio5s = 0f,
            hasIdleRatio5s = false,
            scoreEma = 0f,
            hasScoreEma = false,
            enemiesLeaked = enemiesLeaked,
            combatSeconds = (float)combatSeconds
        };

        matchData.samples.Add(sample);
        AppendSnapshot(sample);
        SaveFile();
    }

    private static void PruneOlderThan(Queue<double> values, double cutoff)
    {
        while (values.Count > 0 && values.Peek() < cutoff)
            values.Dequeue();
    }

    private void PruneActions(double cutoff)
    {
        while (userActions.Count > 0 && userActions.Peek().timestamp < cutoff)
            userActions.Dequeue();
    }

    private static int CountSince(Queue<double> values, double cutoff)
    {
        int count = 0;
        foreach (double timestamp in values)
        {
            if (timestamp >= cutoff)
                count++;
        }
        return count;
    }

    private int CountInvalidActions()
    {
        int count = 0;
        foreach (TimedAction action in userActions)
        {
            if (action.invalid)
                count++;
        }
        return count;
    }

    private static int CountLivingEnemies()
    {
        EnemyStatus[] enemies = FindObjectsByType<EnemyStatus>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        int count = 0;
        foreach (EnemyStatus enemy in enemies)
        {
            if (enemy != null && enemy.IsAlive)
                count++;
        }
        return count;
    }

    private static int CountTowers()
    {
        TowerShoot[] towers = FindObjectsByType<TowerShoot>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        return towers.Length;
    }

    private void AppendSnapshot(MatchSample sample)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotFilePath));
            File.AppendAllText(snapshotFilePath, SerializeSampleWithNulls(sample) + "\n");
        }
        catch (Exception exception)
        {
            Debug.LogError("Không append được snapshot JSONL:\n" + exception.Message, this);
        }
    }

    private void SaveFile()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, SerializeMatchDataWithNulls());
        }
        catch (Exception exception)
        {
            Debug.LogError("Không lưu được thống kê trận đấu:\n" + exception.Message, this);
        }
    }

    private string SerializeMatchDataWithNulls()
    {
        string json = JsonUtility.ToJson(matchData, false);
        json = SetJsonNullWhenMissing(json, nameof(MatchData.playerIdHash), string.IsNullOrEmpty(matchData.playerIdHash));
        json = SetJsonNullWhenMissing(json, nameof(MatchData.patchId), string.IsNullOrEmpty(matchData.patchId));
        json = SetJsonNullWhenMissing(json, nameof(MatchData.endedAtUtc), string.IsNullOrEmpty(matchData.endedAtUtc));

        const string samplesKey = "\"samples\":";
        int keyIndex = json.IndexOf(samplesKey, StringComparison.Ordinal);
        if (keyIndex < 0)
            return json;

        int arrayStart = json.IndexOf('[', keyIndex + samplesKey.Length);
        if (arrayStart < 0)
            return json;

        int arrayEnd = FindMatchingArrayEnd(json, arrayStart);
        if (arrayEnd < 0)
            return json;

        StringBuilder output = new StringBuilder(json.Length + matchData.samples.Count * 16);
        output.Append(json, 0, arrayStart + 1);
        for (int i = 0; i < matchData.samples.Count; i++)
        {
            if (i > 0)
                output.Append(',');
            output.Append(SerializeSampleWithNulls(matchData.samples[i]));
        }
        output.Append(json, arrayEnd, json.Length - arrayEnd);
        return output.ToString();
    }

    private static string SerializeSampleWithNulls(MatchSample sample)
    {
        string json = JsonUtility.ToJson(sample, false);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.playerIdHash), string.IsNullOrEmpty(sample.playerIdHash));
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.patchId), string.IsNullOrEmpty(sample.patchId));
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.segmentId), string.IsNullOrEmpty(sample.segmentId));
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.wave), !sample.hasWave);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.currentDifficulty), !sample.hasCurrentDifficulty);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.progressRatio), !sample.hasProgressRatio);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.hp), !sample.hasHp);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.hpMax), !sample.hasHpMax);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.gold), !sample.hasGold);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.towerCount), !sample.hasTowerCount);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.enemyAlive), !sample.hasEnemyAlive);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.kills5s), !sample.hasKills5s);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.hitsTaken5s), !sample.hasHitsTaken5s);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.hitsTaken30s), !sample.hasHitsTaken30s);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.deaths30s), !sample.hasDeaths30s);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.enemyLeaks30s), !sample.hasEnemyLeaks30s);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.actionCount5s), !sample.hasActionCount5s);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.invalidActionRate), !sample.hasInvalidActionRate);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.idleRatio5s), !sample.hasIdleRatio5s);
        json = SetJsonNullWhenMissing(json, nameof(MatchSample.scoreEma), !sample.hasScoreEma);
        return json;
    }

    private static string SetJsonNullWhenMissing(string json, string fieldName, bool missing)
    {
        if (!missing)
            return json;

        string key = "\"" + fieldName + "\"";
        int keyIndex = json.IndexOf(key, StringComparison.Ordinal);
        while (keyIndex >= 0)
        {
            int previous = keyIndex - 1;
            while (previous >= 0 && char.IsWhiteSpace(json[previous]))
                previous--;
            if (previous >= 0 && (json[previous] == '{' || json[previous] == ','))
                break;
            keyIndex = json.IndexOf(key, keyIndex + key.Length, StringComparison.Ordinal);
        }
        if (keyIndex < 0)
        {
            int objectEnd = json.LastIndexOf('}');
            if (objectEnd < 0)
                return json;
            int previous = objectEnd - 1;
            while (previous >= 0 && char.IsWhiteSpace(json[previous]))
                previous--;
            string separator = previous >= 0 && json[previous] == '{' ? string.Empty : ",";
            return json.Insert(objectEnd, separator + key + ":null");
        }

        int valueStart = json.IndexOf(':', keyIndex + key.Length);
        if (valueStart < 0)
            return json;

        valueStart++;
        while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
            valueStart++;

        int valueEnd = valueStart;
        if (valueStart < json.Length && json[valueStart] == '"')
        {
            valueEnd++;
            bool escaped = false;
            while (valueEnd < json.Length)
            {
                char c = json[valueEnd++];
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    break;
            }
        }
        else
        {
            while (valueEnd < json.Length && json[valueEnd] != ',' && json[valueEnd] != '}' && json[valueEnd] != ']')
                valueEnd++;
            while (valueEnd > valueStart && char.IsWhiteSpace(json[valueEnd - 1]))
                valueEnd--;
        }

        return json.Substring(0, valueStart) + "null" + json.Substring(valueEnd);
    }

    private static int FindMatchingArrayEnd(string json, int arrayStart)
    {
        int depth = 0;
        bool insideString = false;
        bool escaped = false;

        for (int i = arrayStart; i < json.Length; i++)
        {
            char c = json[i];
            if (insideString)
            {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    insideString = false;
                continue;
            }

            if (c == '"')
                insideString = true;
            else if (c == '[')
                depth++;
            else if (c == ']' && --depth == 0)
                return i;
        }

        return -1;
    }

    // Gọi trước khi Restart hoặc về Main Menu.
    public void EndBeforeLeaving()
    {
        if (!initialized)
            return;

        if (!finished)
        {
            string result = GameManager.Instance != null && GameManager.Instance.Hp <= 0
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
            CaptureSample("ApplicationPause");
    }

    private void OnApplicationQuit()
    {
        if (!initialized)
            return;

        if (!finished)
            FinishMatch("Quit");
        else
            SaveFile();
    }

    private void OnDisable()
    {
        EnemyMove.OnEnemyReachEnd -= HandleEnemyReachEnd;
        EnemySpawn.OnAllWavesCompleted -= HandleAllWavesCompleted;
        EnemyStatus.enemyDie -= HandleEnemyDie;
        GameManager.OnPlayerHit -= HandlePlayerHit;
        TelemetryEventLogger.UserActionRecorded -= HandleUserAction;
    }

    private void OnDestroy()
    {
        if (initialized && !finished)
            FinishMatch("Left");
    }
}
