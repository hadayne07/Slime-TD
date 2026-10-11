using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "Scriptable Objects/TowerData")]
public class TowerData : ScriptableObject
{
    [SerializeField] private float speed;
    public float Speed { get => speed; }
    [SerializeField] private float damage;
    public float Damage { get => damage; }
    [SerializeField] private float range;
    public float Range { get => range; }
    [SerializeField] private float timeBulletActive;
    public float TimeBulletActive { get => timeBulletActive; }
}
