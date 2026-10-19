using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [SerializeField] private float speed;
    public float Speed => speed;

    [SerializeField] private int hp;
    public int Hp => hp;
   
    [SerializeField] protected int damage;
    public int Damage { get => damage; }

    [SerializeField] protected int goldFall;
    public int GoldFall => goldFall;
}
