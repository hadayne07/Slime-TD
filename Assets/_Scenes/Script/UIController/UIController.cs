using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    [Header("Thông tin")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private EnemySpawn enemySpawn;
    [SerializeField] private MatchRecorder matchRecorder;

    [Header("Panel")]
    [SerializeField] private GameObject towerPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject LosePanel;
    [SerializeField] private GameObject UpgradePanel;
    [SerializeField] private GameObject winPanel;

    [Header("Đặt tháp")]
    [SerializeField]
    private TowerPlacementManager placementManager;

    [Header("Thông báo")]
    [SerializeField] private TMP_Text warningText;

    [Header("Nút tốc độ và bắt đầu")]
    [SerializeField] private Button SpeedButton05;
    [SerializeField] private Button SpeedButton1;
    [SerializeField] private Button SpeedButton2;
    [SerializeField] private Button StartButton;

    [Header("Màu nút")]
    [SerializeField] private Color normalButton = Color.white;
    [SerializeField] private Color selectedButton = Color.blue;
    [SerializeField] private Color normalText = Color.black;
    [SerializeField] private Color selectedText = Color.white;

    private bool isGamePause;
    private bool isGameEnded;

    private TowerShoot selectedTower;
    private Coroutine warningCoroutine;

    public bool IsGameplayBlocked =>isGameEnded || isGamePause || IsPanelOpen(UpgradePanel);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetPanelActive(pausePanel, false);
        SetPanelActive(LosePanel, false);
        SetPanelActive(UpgradePanel, false);
        SetPanelActive(winPanel, false);

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }

        if (SpeedButton05 != null)
        {
            SpeedButton05.onClick.AddListener(SetSpeed05);
        }

        if (SpeedButton1 != null)
        {
            SpeedButton1.onClick.AddListener(SetSpeed1);
        }

        if (SpeedButton2 != null)
        {
            SpeedButton2.onClick.AddListener(SetSpeed2);
        }
    }

    private void OnEnable()
    {
        
        EnemySpawn.OnWaveChanged += UpdateWaveText;
        EnemySpawn.OnAllWavesCompleted += HandleAllWavesCompleted;
        GameManager.CurrentHp += UpdateHp;
        GameManager.CurrentGold += UpdateGold;
        TowerPlacementManager.CantBuildTower += CantBuild;

        RefreshUI();
    }

    private void Start()
    {
        RefreshUI();
        ApplyTimeScale();

        if (GameManager.Instance != null)
        {
            HighlightButtonSpeed(GameManager.Instance.GameSpeed);
        }
    }

    private void OnDisable()
    {
        EnemySpawn.OnWaveChanged -= UpdateWaveText;
        EnemySpawn.OnAllWavesCompleted -= HandleAllWavesCompleted;
        TowerPlacementManager.CantBuildTower -= CantBuild;
        GameManager.CurrentHp -= UpdateHp;
        GameManager.CurrentGold -= UpdateGold;
    }

    private void OnDestroy()
    {
        if (SpeedButton05 != null)
        {
            SpeedButton05.onClick.RemoveListener(SetSpeed05);
        }

        if (SpeedButton1 != null)
        {
            SpeedButton1.onClick.RemoveListener(SetSpeed1);
        }

        if (SpeedButton2 != null)
        {
            SpeedButton2.onClick.RemoveListener(SetSpeed2);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void RefreshUI()
    {
        if (GameManager.Instance != null)
        {
            UpdateHp(GameManager.Instance.Hp);
            UpdateGold(GameManager.Instance.Gold);
        }

        if (enemySpawn != null)
        {
            UpdateWaveText(enemySpawn.CurrentWaveIndex);
        }
    }

    private void UpdateWaveText(int currentWave)
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {currentWave + 1}";
        }
    }

    private void UpdateHp(int hp)
    {
        if (hpText != null)
        {
            hpText.text = $"HP: {hp}";
        }

        if (hp <= 0 && !isGameEnded)
        {
            ShowPanel(LosePanel);
        }
    }

    private void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"Gold: {gold}";
        }
    }

    private void HandleAllWavesCompleted()
    {
        if (isGameEnded) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.Hp <= 0)
        {
            ShowPanel(LosePanel);
            return;
        }

        ShowPanel(winPanel);
    }

    public void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        if (panel == LosePanel || panel == winPanel)
        {
            if (isGameEnded) return;

            isGameEnded = true;
            isGamePause = false;
            selectedTower = null;

            SetPanelActive(pausePanel, false);
            SetPanelActive(UpgradePanel, false);
        }
        else
        {
            if (isGameEnded) return;

            if (panel == pausePanel)
            {
                isGamePause = true;
            }
        }

        panel.SetActive(true);

        if (IsGameplayBlocked && placementManager != null)
        {
            placementManager.CancelPlacement();
        }

        ApplyTimeScale();
    }

    public void HidePanel(GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(false);

        if (panel == pausePanel)
        {
            isGamePause = false;
        }

        if (panel == UpgradePanel)
        {
            selectedTower = null;
        }

        ApplyTimeScale();
    }

    public void TogglePause()
    {
        if (isGameEnded) return;
        if (IsPanelOpen(UpgradePanel)) return;

        if (isGamePause)
        {
            HidePanel(pausePanel);
        }
        else
        {
            ShowPanel(pausePanel);
        }
    }

    public void ShowTowerPanel()
    {
        if (isGameEnded) return;

        SetPanelActive(towerPanel, true);
    }

    public void HideTowerPanel()
    {
        if (placementManager != null)
        {
            placementManager.CancelPlacement();
        }

        SetPanelActive(towerPanel, false);
    }

    private void ApplyTimeScale()
    {
        if (GameManager.Instance == null) return;

        float scale = IsGameplayBlocked? 0f: GameManager.Instance.GameSpeed;

        GameManager.Instance.SetTimeScale(scale);
    }

    private static bool IsPanelOpen(GameObject panel)
    {
        return panel != null && panel.activeSelf;
    }

    private static void SetPanelActive(GameObject panel,bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }


    public void SelectTower(TowerShoot tower)
    {
        if (tower == null || IsGameplayBlocked)
        {
            return;
        }

        selectedTower = tower;

        ShowPanel(UpgradePanel);
    }

    public void UpgradeSelectedTower()
    {
        if (isGameEnded) return;

        if (selectedTower == null)
        {
            ShowWarning("Chưa chọn tháp.");
            HidePanel(UpgradePanel);
            return;
        }

        bool upgraded = selectedTower.TryUpgrade();

        ShowWarning( upgraded ? "Nâng cấp thành công." : "Không đủ vàng hoặc tháp đã đạt cấp tối đa.");

        HidePanel(UpgradePanel);
    }

    public void NoButtonUpgradePanel()
    {
        HidePanel(UpgradePanel);
    }

    public void ShowWarning(string message)
    {
        if (warningText == null) return;

        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }

        warningCoroutine = StartCoroutine(ShowMess(message));
    }

    private IEnumerator ShowMess(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(1f);

        warningText.gameObject.SetActive(false);
        warningCoroutine = null;
    }

    private void SetSpeed05()
    {
        SetTimeSpeed(0.5f);
    }

    private void SetSpeed1()
    {
        SetTimeSpeed(1f);
    }

    private void SetSpeed2()
    {
        SetTimeSpeed(2f);
    }

    private void SetTimeSpeed(float speed)
    {
        if (IsGameplayBlocked) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.SetGameSpeed(speed);

        HighlightButtonSpeed(speed);
    }

    private void UpdateButtonVisual(Button button, bool isSelected )
    {
        if (button == null) return;

        if (button.image != null)
        {
            button.image.color = isSelected   ? selectedButton: normalButton;
        }

        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>();

        if (tmp != null)
        {
            tmp.color = isSelected ? selectedText: normalText;
        }
    }

    private void HighlightButtonSpeed(float speed)
    {
        UpdateButtonVisual( SpeedButton05, Mathf.Approximately(speed, 0.5f));
        UpdateButtonVisual(SpeedButton1, Mathf.Approximately(speed, 1f)  );
        UpdateButtonVisual(SpeedButton2, Mathf.Approximately(speed, 2f));
    }

    public void RestartLevel()
    {
        if (matchRecorder != null)
        {
            matchRecorder.EndBeforeLeaving();
        }
        if (placementManager != null)
        {
            placementManager.CancelPlacement();
        }

        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();

        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void GoMainMenu()
    {
        if (matchRecorder != null)
        {
            matchRecorder.EndBeforeLeaving();
        }
        if (placementManager != null)
        {
            placementManager.CancelPlacement();
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene("Main Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void CantBuild(TowerData data)
    {
        ShowWarning("Không đủ vàng");
    }
    public void StartWave()
    {
        if (IsGameplayBlocked || enemySpawn == null ||!enemySpawn.CanStartNextWave)
        {
            return;
        }

        enemySpawn.TriggerStartWave();
        RefreshStartButton();
    }
    private void Update()
    {
        RefreshStartButton();
    }

    private void RefreshStartButton()
    {
        if (StartButton == null)
            return;

        bool canStart =
            !IsGameplayBlocked &&
            enemySpawn != null &&
            enemySpawn.CanStartNextWave;

        StartButton.interactable = canStart;
    }
}