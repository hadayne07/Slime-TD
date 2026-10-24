using UnityEngine;

[CreateAssetMenu(fileName = "TowerData", menuName = "Scriptable Objects/TowerData")]
public class TowerData : ScriptableObject
{
    [SerializeField] private float speedBulletlv1;
    public float Speedlv1 { get => speedBulletlv1; }
    [SerializeField] private float speedBulletlv2;
    public float Speedlv2 { get => speedBulletlv2; }
    [SerializeField] private int damagelv1;
    public int Damagelv1 { get => damagelv1; }
    [SerializeField] private int damagelv2;
    public int Damagelv2 { get => damagelv2; }
    [SerializeField] private float range;
    public float Range { get => range; }
    [SerializeField] private float timeShoot;
    public float TimeShoot { get => timeShoot; }
    [SerializeField] private float bulletSize;
    public float BulletSize => bulletSize;

    [SerializeField] private int cost;
    public int Cost => cost;
    [SerializeField] private int upgradecost;
    public int UpgradeCost => upgradecost;

    [SerializeField] private Sprite spritelv1;
    public Sprite Spritelv1 => spritelv1;
    [SerializeField] private Sprite spritelv2;
    public Sprite Spritelv2 => spritelv2;
    [SerializeField] private float timeEffectlv1;
    public float TimeEffectlv1 => timeEffectlv1;
    [SerializeField] private float timeEffectlv2;
    public float TimeEffectlv2 => timeEffectlv2;
    [SerializeField] private float damageEffectlv1;
    public float DamageEffectlv1 => damageEffectlv1;
    [SerializeField] private float damageEffectlv2;
    public float DamageEffectlv2 => damageEffectlv2;

    public GameObject placementPreviewPrefab;

    public GameObject Prefab;
}
