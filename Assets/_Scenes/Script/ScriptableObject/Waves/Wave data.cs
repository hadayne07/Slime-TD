using UnityEngine;

[CreateAssetMenu(fileName = "Wavedata", menuName = "Scriptable Objects/Wavedata")]
public class Wavedata : ScriptableObject
{
    [SerializeField] public EnemyType enemy;
   public float timeSpawn;

    public int enemyPerWave;

}
