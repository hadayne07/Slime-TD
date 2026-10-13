using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [SerializeField] protected float speed;
    public float Speed { get => speed;}
    [SerializeField] public float hp;
   
    [SerializeField] protected int damage;
    public int Damage { get => damage; }
}
