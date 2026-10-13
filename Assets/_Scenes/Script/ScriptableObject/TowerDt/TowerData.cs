using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "Scriptable Objects/TowerData")]
public class TowerData : ScriptableObject
{
    [SerializeField] private float speedBullet;
    public float Speed { get => speedBullet; }
    [SerializeField] private int damage;
    public int Damage { get => damage; }
    [SerializeField] private float range;
    public float Range { get => range; }
    [SerializeField] private float timeShoot;
    public float TimeShoot { get => timeShoot; }


}
