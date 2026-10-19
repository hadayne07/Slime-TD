using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Scene = UnityEngine.SceneManagement.Scene;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text goldText;

    [SerializeField] private GameObject towerPanel;

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject LosePanel;
    [SerializeField] private GameObject UpgradePanel;
    [SerializeField] private GameObject WinPanel;
    private bool isGamePause = false;
    private TowerShoot selectedTower;


    [SerializeField] private GameObject towerCardPrefab;

    [SerializeField] private Transform towerCardContainer;

    [SerializeField] private TMP_Text warningText;

    [SerializeField] private Button SpeedButton05;
    [SerializeField] private Button SpeedButton1;
    [SerializeField] private Button SpeedButton2;

    [SerializeField]private Color normalButton = Color.white;
    [SerializeField]private Color selectedButton = Color.blue;
    [SerializeField]private Color normalText = Color.black;
    [SerializeField]private Color selectedText = Color.white;
    private Platform currentPlatform;


    [SerializeField] private TowerData[] towers;
    private List<GameObject> activeCard = new List<GameObject>();

    public static UIController Instance { get; private set; }

  
    private void OnEnable()
    {
        EnemySpawn.OnWaveChanged += UpdateWaveText;
        GameManager.CurrentHp += UpdateHp;
        GameManager.CurrentGold += UpdateGold;
        Platform.OnclickPlatform += HandleOnClickPlatform;
        TowerCard.OnTowerSelected += HandleSelectedTower;
    }

    private void OnDisable()
    {

        EnemySpawn.OnWaveChanged -= UpdateWaveText;
        GameManager.CurrentHp -= UpdateHp;
        GameManager.CurrentGold -= UpdateGold;
        Platform.OnclickPlatform -= HandleOnClickPlatform;
        TowerCard.OnTowerSelected -= HandleSelectedTower;
    }

    private void Awake()
    {

        if ( Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        towerPanel.SetActive(false);
        pausePanel.SetActive(false);
   
        SpeedButton05.onClick.AddListener(() => SetTimeSpeed(0.5f));
        SpeedButton1.onClick.AddListener(() => SetTimeSpeed(1f));
        SpeedButton2.onClick.AddListener(() => SetTimeSpeed(2f));
    }

    

    private void UpdateWaveText(int currentWave)
    {
        waveText.text = $"Wave: {currentWave + 1}/30";
        if(currentWave==30)
        {
            ShowPanel(WinPanel);
        }
    }

    private void UpdateHp( int hp)
    {
        hpText.text = $"HP: {hp}";
        if (GameManager.Instance.Hp <= 0) ShowPanel(LosePanel);
    }
    private void UpdateGold(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }

    public void ShowTowerPanel()
    {
        Platform.OnPlatformOpen = true;
        ShowPanel(towerPanel);
        PopulateTowerCard();
    }

    public void HideTowerPanel()
    {
        HidePanel(towerPanel);
        Platform.OnPlatformOpen = false;
       
    }

    public void ShowPanel(GameObject panel)
    {
        GameManager.Instance.SetTimeScale(0f);
        panel.SetActive(true);
    }
    public void HidePanel(GameObject panel)
    {
        GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);
        panel.SetActive(false);
    }

    public void TogglePause()
    {
        if (towerPanel.activeSelf)
            return;
        if (isGamePause)
        {
            HidePanel(pausePanel);
            isGamePause = false;
        }
        else
        {
          ShowPanel(pausePanel);
            isGamePause = true;
        }
    }
    private void HandleOnClickPlatform(Platform platform)
    {
        
        currentPlatform = platform;
        selectedTower = platform.TowerInstance;
        if (selectedTower != null)
        {
            ShowPanel(UpgradePanel);
            return;
        }
        ShowTowerPanel();
    }

    private void PopulateTowerCard()
    {
        
        foreach ( var card in activeCard)
        {
            
            Destroy(card);
        }
        activeCard.Clear();

        foreach ( var data in towers)
        {
          
            GameObject cardGameObject = Instantiate(towerCardPrefab, towerCardContainer);
            TowerCard card = cardGameObject.GetComponent<TowerCard>();
            card.Initialize(data);
            activeCard.Add(cardGameObject);
            
         
        }
    }


    protected void HandleSelectedTower(TowerData data)
    {
        if (currentPlatform.transform.childCount > 0)
        {
            StartCoroutine(ShowMess(" This Place Already have tower!"));
            HideTowerPanel();
            return;
        }
        if (GameManager.Instance.Gold >= data.Cost)
        {
            currentPlatform.BuildTower(data);
            HideTowerPanel();
        }

        else
        {
          
            StartCoroutine(ShowMess(" Not Enough Gold"));
            HideTowerPanel();
        }

    }

    private IEnumerator ShowMess( string mess)
    {
        warningText.text = mess;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(1f);
        warningText.gameObject.SetActive(false);
    }

    private void SetTimeSpeed ( float time)
    {
        HighlightButtonSpeed(time);
        GameManager.Instance.SetGameSpeed(time);
    }

    private void UpdateButtonVisual(Button button , bool isSelected)
    {
        button.image.color = isSelected ? selectedButton : normalButton;

        TMP_Text tmp = GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.color = isSelected ? selectedText : normalText;
        }
    }

    private void HighlightButtonSpeed ( float speed)
    {
        UpdateButtonVisual(SpeedButton05, speed == 0.5f);
        UpdateButtonVisual(SpeedButton1, speed == 1f);
        UpdateButtonVisual(SpeedButton2, speed == 2f);
    }

   

    public void RestartLevel()
    {
        GameManager.Instance.SetTimeScale(1f);
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
       
    }

    public void UpgradeSelectedTower()
    {
       if (selectedTower.TryUpgrade() == false)
        {
            StartCoroutine(ShowMess(" Not Enough Gold or MaxLevel"));
            HidePanel(UpgradePanel);
            return;
        }

        StartCoroutine(ShowMess(" UpgradeSuccesful"));
        HidePanel(UpgradePanel);
    }
   public void NoButtonUpgradePanel()
    {
        Platform.OnPlatformOpen = false;
        HidePanel(UpgradePanel);
        selectedTower = null;
        currentPlatform = null;

    }
}

