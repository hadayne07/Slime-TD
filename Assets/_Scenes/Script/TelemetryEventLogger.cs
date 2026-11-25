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

    private string filePath;
    private double startedAtReal;
    private string activeSegmentId;
    private string currentMapId;
    private bool segmentOpen;
    private bool sessionEnded;

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
        currentMapId = scene.name;
        startedAtReal = Time.realtimeSinceStartupAsDouble;

        string directory = Path.Combine(Application.persistentDataPath, "Telemetry");
        string fileName = string.Format(
            "events_{0:yyyyMMdd_HHmmss}_{1}.jsonl",
            DateTime.UtcNow,
            SessionId
        );
        filePath = Path.Combine(directory, fileName);

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
        if (Instance == null || Instance.sessionEnded)
            return;

        Instance.Record("player_hit", record =>
        {
            record.damage = damage;
            record.hasDamage = true;
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

        if (!Instance.segmentOpen)
        {
            Instance.activeSegmentId = string.Format("wave-{0:00}", wave);
            Instance.segmentOpen = true;
        }

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
            string line = JsonUtility.ToJson(record, false);
            File.AppendAllText(filePath, line + "\n");
        }
        catch (Exception exception)
        {
            Debug.LogError("Không ghi được telemetry event: " + exception.Message, this);
        }
    }
}
