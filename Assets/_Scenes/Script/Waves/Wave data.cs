using UnityEngine;

[CreateAssetMenu(fileName = "Wavedata", menuName = "Scriptable Objects/Wavedata")]
public class Wavedata : ScriptableObject
{

    public EnemyGroup[] groups;
    [SerializeField] private float timeNextWave;
    public float TimeNextWave => timeNextWave;

}
