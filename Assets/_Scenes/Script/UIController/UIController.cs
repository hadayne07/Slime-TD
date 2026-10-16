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
    private bool isGamePause = false;


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
        GameManager.CurrentGold += UpdateGold;
        Platform.OnclickPlatform -= HandleOnClickPlatform;
        TowerCard.OnTowerSelected -= HandleSelectedTower;
    }

    private void Awake()
    {
        towerPanel.SetActive(false);
        pausePanel.SetActive(false);
   
        SpeedButton05.onClick.AddListener(() => SetTimeSpeed(0.5f));
        SpeedButton1.onClick.AddListener(() => SetTimeSpeed(1f));
        SpeedButton2.onClick.AddListener(() => SetTimeSpeed(2f));
    }

    

    private void UpdateWaveText(int currentWave)
    {
        waveText.text = $"Wave: {currentWave + 1}";
    }

    private void UpdateHp( int hp)
    {
        hpText.text = $"HP: {hp}";
        if (GameManager.Instance.Hp <= 0) ShowLosePanel();
    }
    private void UpdateGold(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }

    public void ShowTowerPanel()
    {
        Platform.OnPlatformOpen = true;
        towerPanel.SetActive(true);
        GameManager.Instance.SetTimeScale(0f);
        PopulateTowerCard();
    }

    public void HideTowerPanel()
    {
        towerPanel.SetActive(false);
        Platform.OnPlatformOpen = false;
        GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);
    }

    public void ShowLosePanel()
    {
        GameManager.Instance.SetTimeScale(0f);
        LosePanel.SetActive(true);
    }
    private void HandleOnClickPlatform(Platform platform)
    {

        currentPlatform = platform;
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

    public void TogglePause()
    {
        if (towerPanel.activeSelf)
            return;
        if ( isGamePause)
        {
            pausePanel.SetActive(false);
            GameManager.Instance.SetTimeScale(GameManager.Instance.GameSpeed);
            isGamePause = false;
        }
        else
        {
            pausePanel.SetActive(true);
            GameManager.Instance.SetTimeScale(0f);
            isGamePause = true;
        }
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
        Debug.Log("RUn");
    }
   
}

