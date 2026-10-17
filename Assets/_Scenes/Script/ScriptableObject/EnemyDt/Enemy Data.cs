using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [SerializeField] public float speed;

    [SerializeField] public float hp;
   
    [SerializeField] protected int damage;
    public int Damage { get => damage; }

    [SerializeField] protected int goldFall;
    public int GoldFall => goldFall;
}
