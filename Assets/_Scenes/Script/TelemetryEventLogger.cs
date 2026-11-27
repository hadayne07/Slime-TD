using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Append-only JSON Lines logger for gameplay events.
/// It bootstraps before the first scene, so it does not need manual scene wiring.
/// </summary>
public sealed class TelemetryEventLogger : MonoBehaviour
{
    private const string SchemaVersion = "telemetry-v1";
    private const string DefaultObjectiveId = "defend_core";

    public static TelemetryEventLogger Instance { get; private set; }
    public static string SessionId { get; private set; }
    public static event Action<bool> UserActionRecorded;

    [Serializable]
    private sealed class EventRecord
    {
        public string schemaVersion;
        public string eventId;
        public string eventType;
        public string sessionId;
        public string playerIdHash;
        public string runId;
        public string matchId;
        public string mapId;
        public string patchId;
        public string clientBuild;
        public string deviceClass;
        public string recordedAtUtc;
        public float elapsedRealSeconds;
        public string segmentId;
        public string objectiveId;

        public string reason;
        public string source;
        public string outcomeRaw;
        public string enemyType;
        public string attackerType;
        public string actionType;
        public string objectType;
        public string policyMode;
        public string controllerVersion;
        public string modelVersion;
        public string decisionId;
        public string difficultyVectorJson;
        public string difficultyMaskJson;
        public string positionBucket;
        public string candidateSetJson;

        public int wave;
        public bool hasWave;
        public int count;
        public bool hasCount;
        public int hp;
        public bool hasHp;
        public int hpMax;
        public bool hasHpMax;
        public float damage;
        public bool hasDamage;
        public int hpLost;
        public bool hasHpLost;
        public int enemiesLeaked;
        public int enemiesLeakedGreen;
        public int enemiesLeakedBlue;
        public int enemiesLeakedRed;
        public int enemiesLeakedBoss;
        public bool hasLeakTotals;
        public int gold;
        public bool hasGold;
        public int goldDelta;
        public bool hasGoldDelta;
        public int cost;
        public bool hasCost;
        public int towerLevel;
        public bool hasTowerLevel;
        public int currentDifficulty;
        public bool hasCurrentDifficulty;
        public int candidateDifficulty;
        public bool hasCandidateDifficulty;
        public int appliedDifficulty;
        public bool hasAppliedDifficulty;
        public int randomSeed;
        public bool hasRandomSeed;
        public float propensity;
        public bool hasPropensity;
        public bool safeCheckpoint;
        public bool hasSafeCheckpoint;
        public bool terminal;
        public bool hasTerminal;
    }

    private struct LeakTotals
    {
        public int hpLost;
        public int total;
        public int green;
        public int blue;
        public int red;
        public int boss;

        public void Add(EnemyType type, int lost)
        {
            hpLost += lost;
            total++;

            switch (type)
            {
                case EnemyType.Green: green++; break;
                case EnemyType.Blue: blue++; break;
                case EnemyType.Red: red++; break;
                case EnemyType.Boss: boss++; break;
            }
        }

        public void WriteTo(EventRecord record)
        {
            record.hpLost = hpLost;
            record.hasHpLost = true;
            record.enemiesLeaked = total;
            record.enemiesLeakedGreen = green;
            record.enemiesLeakedBlue = blue;
            record.enemiesLeakedRed = red;
            record.enemiesLeakedBoss = boss;
            record.hasLeakTotals = true;
        }
    }

    private string filePath;
    private double startedAtReal;
    private string activeSegmentId;
    private string currentMapId;
    private bool segmentOpen;
    private bool sessionEnded;
    private LeakTotals segmentLeaks;
    private LeakTotals sessionLeaks;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        SessionId = Guid.NewGuid().ToString("N");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject loggerObject = new GameObject("TelemetryEventLogger");
        loggerObject.AddComponent<TelemetryEventLogger>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (string.IsNullOrEmpty(SessionId))
            SessionId = Guid.NewGuid().ToString("N");

        Scene activeScene = SceneManager.GetActiveScene();
        if (IsGameplayScene(activeScene))
        {
            BeginSession(activeScene);
            EnsureMatchRecorder(activeScene);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsGameplayScene(scene))
            return;

        bool newMatch = sessionEnded || string.IsNullOrEmpty(filePath) || currentMapId != scene.name;
        if (newMatch)
        {
            if (!sessionEnded && !string.IsNullOrEmpty(filePath))
                EndSession("left", "left");

            SessionId = Guid.NewGuid().ToString("N");
            BeginSession(scene);
        }

        EnsureMatchRecorder(scene);
    }

    private static bool IsGameplayScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded ||
            string.IsNullOrEmpty(scene.name) ||
            !scene.name.StartsWith("Map", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool hasGameManager = false;
        bool hasEnemySpawn = false;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<GameManager>(true) != null)
                hasGameManager = true;
            if (root.GetComponentInChildren<EnemySpawn>(true) != null)
                hasEnemySpawn = true;
        }

        return hasGameManager && hasEnemySpawn;
    }

    private static void EnsureMatchRecorder(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<MatchRecorder>(true) != null)
                return;
        }

        GameObject recorderObject = new GameObject("MatchRecorder (Auto)");
        if (recorderObject.scene != scene)
            SceneManager.MoveGameObjectToScene(recorderObject, scene);
        recorderObject.AddComponent<MatchRecorder>();
    }

    private void BeginSession(Scene scene)
    {
        sessionEnded = false;
        segmentOpen = false;
        activeSegmentId = null;
        segmentLeaks = default;
        sessionLeaks = default;
        currentMapId = scene.name;
        startedAtReal = Time.realtimeSinceStartupAsDouble;

        string directory = System.IO.Path.Combine(Application.persistentDataPath, "Telemetry");
        string fileName = string.Format(
            "events_{0:yyyyMMdd_HHmmss}_{1}.jsonl",
            DateTime.UtcNow,
            SessionId
        );
        filePath = System.IO.Path.Combine(directory, fileName);

        try
        {
            Directory.CreateDirectory(directory);
            Record("session_start", record =>
            {
                record.mapId = scene.name;
                record.clientBuild = Application.version;
                record.deviceClass = SystemInfo.deviceType.ToString().ToLowerInvariant();
            });
            Debug.Log("Telemetry event log: " + filePath, this);
        }
        catch (Exception exception)
        {
            Debug.LogError("Không khởi tạo được telemetry event log: " + exception.Message, this);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        EndSession("quit", "quit");
    }

    public static void RecordSegmentStart(int wave)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.activeSegmentId = string.Format("wave-{0:00}", wave);
        Instance.segmentLeaks = default;
        Instance.segmentOpen = true;
        Instance.Record("segment_start", record =>
        {
            record.segmentId = Instance.activeSegmentId;
            record.objectiveId = DefaultObjectiveId;
            record.wave = wave;
            record.hasWave = true;
        });
        UserActionRecorded?.Invoke(false);
    }

    public static void RecordEnemySpawn(EnemyType enemyType, int wave, Vector3 position)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("enemy_spawn", record =>
        {
            record.enemyType = enemyType.ToString();
            record.count = 1;
            record.hasCount = true;
            record.wave = wave;
            record.hasWave = true;
            record.positionBucket = MakePositionBucket(position);
        });
    }

    public static void RecordEnemyKill(EnemyType enemyType)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("enemy_kill", record =>
        {
            record.enemyType = enemyType.ToString();
            record.attackerType = "unknown";
        });
    }

    public static void RecordPlayerHit(int damage, int hpAfter)
    {
        WritePlayerHit(damage, hpAfter, null, null);
    }

    public static void RecordEnemyLeak(EnemyType enemyType, int damage, int hpLost, int hpAfter)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.sessionLeaks.Add(enemyType, hpLost);
        if (Instance.segmentOpen)
            Instance.segmentLeaks.Add(enemyType, hpLost);

        WritePlayerHit(damage, hpAfter, enemyType, hpLost);
    }

    private static void WritePlayerHit(int damage, int hpAfter, EnemyType? enemyType, int? hpLost)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("player_hit", record =>
        {
            record.segmentId = Instance.segmentOpen ? Instance.activeSegmentId : null;
            record.enemyType = enemyType?.ToString();
            record.damage = damage;
            record.hasDamage = true;
            record.hpLost = hpLost.GetValueOrDefault();
            record.hasHpLost = hpLost.HasValue;
            record.hp = hpAfter;
            record.hasHp = true;
            record.source = "enemy_leak";
        });
    }

    public static void RecordDeath()
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("death", record =>
        {
            record.reason = "core_hp_zero";
            record.hp = GameManager.Instance != null ? GameManager.Instance.Hp : 0;
            record.hasHp = true;
        });
        Instance.CloseSegment("lost", true);
        EndSession("lost", "lost");
    }

    public static void RecordTowerBuilt(TowerData towerData, Vector3 position)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("tower_build", record =>
        {
            record.objectType = towerData != null ? towerData.name : null;
            record.cost = towerData != null ? towerData.Cost : 0;
            record.hasCost = towerData != null;
            record.positionBucket = MakePositionBucket(position);
        });
        UserActionRecorded?.Invoke(false);
    }

    public static void RecordTowerUpgrade(TowerData towerData, int level)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("tower_upgrade", record =>
        {
            record.objectType = towerData != null ? towerData.name : null;
            record.towerLevel = level;
            record.hasTowerLevel = true;
            record.cost = towerData != null ? towerData.UpgradeCost : 0;
            record.hasCost = towerData != null;
        });
        UserActionRecorded?.Invoke(false);
    }

    public static void RecordInvalidAction(string actionType, string reason, TowerData towerData = null)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("invalid_action", record =>
        {
            record.actionType = actionType;
            record.reason = reason;
            record.objectType = towerData != null ? towerData.name : null;
            record.cost = towerData == null
                ? 0
                : (actionType == "tower_upgrade" ? towerData.UpgradeCost : towerData.Cost);
            record.hasCost = towerData != null;
        });
        UserActionRecorded?.Invoke(true);
    }

    /// <summary>
    /// Optional hook for a future randomized/adaptive difficulty controller.
    /// The current prototype does not apply difficulty profiles.
    /// </summary>
    public static void RecordDifficultyApplied(
        int currentDifficulty,
        int candidateDifficulty,
        int appliedDifficulty,
        string policyMode,
        float propensity,
        int randomSeed,
        string candidateSetJson,
        bool safeCheckpoint,
        string controllerVersion,
        string difficultyVectorJson,
        string difficultyMaskJson,
        string modelVersion = null)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("difficulty_applied", record =>
        {
            record.currentDifficulty = currentDifficulty;
            record.hasCurrentDifficulty = true;
            record.candidateDifficulty = candidateDifficulty;
            record.hasCandidateDifficulty = true;
            record.appliedDifficulty = appliedDifficulty;
            record.hasAppliedDifficulty = true;
            record.policyMode = policyMode;
            record.propensity = propensity;
            record.hasPropensity = true;
            record.randomSeed = randomSeed;
            record.hasRandomSeed = true;
            record.candidateSetJson = candidateSetJson;
            record.safeCheckpoint = safeCheckpoint;
            record.hasSafeCheckpoint = true;
            record.controllerVersion = controllerVersion;
            record.modelVersion = modelVersion;
            record.decisionId = Guid.NewGuid().ToString("N");
            record.difficultyVectorJson = difficultyVectorJson;
            record.difficultyMaskJson = difficultyMaskJson;
        });
    }

    public static void RecordWaveCompleted(int wave)
    {
        if (Instance == null || Instance.sessionEnded)
            return;

        if (!Instance.segmentOpen || Instance.activeSegmentId != string.Format("wave-{0:00}", wave))
            return;

        Instance.CloseSegment("won", false);
    }

    public static void EndSession(string reason, string outcomeRaw = null)
    {
        if (Instance == null || Instance.sessionEnded || string.IsNullOrEmpty(Instance.filePath))
            return;

        if (Instance.segmentOpen)
            Instance.CloseSegment(outcomeRaw ?? reason, true);

        if (reason == "quit" || outcomeRaw == "quit")
        {
            Instance.Record("quit", record =>
            {
                record.reason = reason;
                record.outcomeRaw = outcomeRaw ?? reason;
            });
        }

        Instance.Record("session_end", record =>
        {
            record.reason = reason;
            record.outcomeRaw = outcomeRaw ?? reason;
            Instance.sessionLeaks.WriteTo(record);
        });
        Instance.sessionEnded = true;
    }

    private void CloseSegment(string outcomeRaw, bool terminal)
    {
        if (!segmentOpen || sessionEnded)
            return;

        if (outcomeRaw == "won" || outcomeRaw == "lost")
        {
            Record(outcomeRaw == "won" ? "objective_success" : "objective_failure", record =>
            {
                record.segmentId = activeSegmentId;
                record.objectiveId = DefaultObjectiveId;
                record.outcomeRaw = outcomeRaw;
                record.terminal = terminal;
                record.hasTerminal = true;
            });
        }

        Record("segment_end", record =>
        {
            record.segmentId = activeSegmentId;
            record.objectiveId = DefaultObjectiveId;
            record.outcomeRaw = outcomeRaw;
            record.terminal = terminal;
            record.hasTerminal = true;
            record.reason = "wave_boundary_or_session_end";
            segmentLeaks.WriteTo(record);
        });
        segmentOpen = false;
    }

    private static string MakePositionBucket(Vector3 position)
    {
        // Coarse spatial bucket only; do not log precise world coordinates.
        int x = Mathf.FloorToInt(position.x / 2f);
        int y = Mathf.FloorToInt(position.y / 2f);
        return string.Format("x{0}_y{1}", x, y);
    }

    private void Record(string eventType, Action<EventRecord> populate = null)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        EventRecord record = new EventRecord
        {
            schemaVersion = SchemaVersion,
            eventId = Guid.NewGuid().ToString("N"),
            eventType = eventType,
            sessionId = SessionId,
            playerIdHash = null,
            runId = SessionId,
            matchId = SessionId,
            mapId = SceneManager.GetActiveScene().name,
            patchId = null,
            recordedAtUtc = DateTime.UtcNow.ToString("O"),
            elapsedRealSeconds = (float)(Time.realtimeSinceStartupAsDouble - startedAtReal),
            segmentId = activeSegmentId,
            objectiveId = DefaultObjectiveId
        };

        populate?.Invoke(record);

        try
        {
            string line = SerializeEventRecordWithNulls(record);
            File.AppendAllText(filePath, line + "\n");
        }
        catch (Exception exception)
        {
            Debug.LogError("Không ghi được telemetry event: " + exception.Message, this);
        }
    }

    private static string SerializeEventRecordWithNulls(EventRecord record)
    {
        string json = JsonUtility.ToJson(record, false);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.playerIdHash), string.IsNullOrEmpty(record.playerIdHash));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.patchId), string.IsNullOrEmpty(record.patchId));

        json = SetJsonNullWhenMissing(json, nameof(EventRecord.segmentId), string.IsNullOrEmpty(record.segmentId));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.reason), string.IsNullOrEmpty(record.reason));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.source), string.IsNullOrEmpty(record.source));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.outcomeRaw), string.IsNullOrEmpty(record.outcomeRaw));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.enemyType), string.IsNullOrEmpty(record.enemyType));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.attackerType), string.IsNullOrEmpty(record.attackerType));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.actionType), string.IsNullOrEmpty(record.actionType));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.objectType), string.IsNullOrEmpty(record.objectType));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.policyMode), string.IsNullOrEmpty(record.policyMode));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.controllerVersion), string.IsNullOrEmpty(record.controllerVersion));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.modelVersion), string.IsNullOrEmpty(record.modelVersion));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.decisionId), string.IsNullOrEmpty(record.decisionId));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.difficultyVectorJson), string.IsNullOrEmpty(record.difficultyVectorJson));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.difficultyMaskJson), string.IsNullOrEmpty(record.difficultyMaskJson));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.positionBucket), string.IsNullOrEmpty(record.positionBucket));
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.candidateSetJson), string.IsNullOrEmpty(record.candidateSetJson));

        json = SetJsonNullWhenMissing(json, nameof(EventRecord.wave), !record.hasWave);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.count), !record.hasCount);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.hp), !record.hasHp);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.hpMax), !record.hasHpMax);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.damage), !record.hasDamage);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.hpLost), !record.hasHpLost);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.enemiesLeaked), !record.hasLeakTotals);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.enemiesLeakedGreen), !record.hasLeakTotals);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.enemiesLeakedBlue), !record.hasLeakTotals);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.enemiesLeakedRed), !record.hasLeakTotals);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.enemiesLeakedBoss), !record.hasLeakTotals);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.gold), !record.hasGold);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.goldDelta), !record.hasGoldDelta);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.cost), !record.hasCost);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.towerLevel), !record.hasTowerLevel);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.currentDifficulty), !record.hasCurrentDifficulty);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.candidateDifficulty), !record.hasCandidateDifficulty);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.appliedDifficulty), !record.hasAppliedDifficulty);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.randomSeed), !record.hasRandomSeed);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.propensity), !record.hasPropensity);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.safeCheckpoint), !record.hasSafeCheckpoint);
        json = SetJsonNullWhenMissing(json, nameof(EventRecord.terminal), !record.hasTerminal);
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
}
