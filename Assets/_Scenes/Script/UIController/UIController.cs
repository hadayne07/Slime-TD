using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text goldText;
    private void OnEnable()
    {
        EnemySpawn.OnWaveChanged += UpdateWaveText;
        GameManager.CurrentHp += UpdateHp;
        GameManager.CurrentGold += UpdateGold;
    }

    private void OnDisable()
    {

        EnemySpawn.OnWaveChanged -= UpdateWaveText;
        GameManager.CurrentHp -= UpdateHp;
        GameManager.CurrentGold -= UpdateGold;
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
}

