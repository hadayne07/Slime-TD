using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private GameObject TowerPanel;
    private void OnEnable()
    {
        EnemySpawn.OnWaveChanged += UpdateWaveText;
        GameManager.CurrentHp += UpdateHp;
        GameManager.CurrentGold += UpdateGold;
        GameManager.CurrentGold += UpdateGold;
        Platform.OnclickPlatform += HandleOnClickPlatform;
    }

    private void OnDisable()
    {

        EnemySpawn.OnWaveChanged -= UpdateWaveText;
        GameManager.CurrentHp -= UpdateHp;
        Platform.OnclickPlatform -= HandleOnClickPlatform;
    }

    


    private void UpdateWaveText(int currentWave)
    {
        waveText.text = $"Wave: {currentWave + 1}";
    }

    private void UpdateHp( int hp)
    {
        hpText.text = $"HP: {hp}";
    }
    private void UpdateGold(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }

    public void ShowTowerPanel()
    {
        TowerPanel.SetActive(true);
        GameManager.Instance.SetTimeScale(0f);
    }

    public void HideTowerPanel()
    {
        TowerPanel.SetActive(false);
        GameManager.Instance.SetTimeScale(1f);
    }
    private void HandleOnClickPlatform(Platform platform)
    {
        ShowTowerPanel();
    }
}

