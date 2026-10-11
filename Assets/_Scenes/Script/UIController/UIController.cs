using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text hpText;
    private void OnEnable()
    {
        EnemySpawn.OnWaveChanged += UpdateWaveText;
        PlayerStatus.CurrentHp += UpdateHp;
    }

    private void OnDisable()
    {

        EnemySpawn.OnWaveChanged -= UpdateWaveText;
        PlayerStatus.CurrentHp -= UpdateHp;
    }

    


    private void UpdateWaveText(int currentWave)
    {
        waveText.text = $"Wave: {currentWave + 1}";
    }

    private void UpdateHp( int hp)
    {
        hpText.text = $"HP {hp}";
    }
}

