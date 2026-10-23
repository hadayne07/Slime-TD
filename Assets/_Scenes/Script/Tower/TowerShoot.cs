using UnityEngine;

public class TowerShoot : MonoBehaviour
{
    [SerializeField] private TowerData data;
    public TowerData Data { get => data; }
    [SerializeField] private ObjectPooler pooler;
       public int currentDame;
      public float currentSpeed;
    public float currentTimeEffect;
    public float currentDamageEffect;
    public SpriteRenderer spriteRenderer;
    public Sprite currentSprite;
    public float sizeBullet;
    [SerializeField] private int currentlevel =1;
    private int maxLevel = 2;



    private TowerTarget target;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        target = GetComponent<TowerTarget>();
        sizeBullet = data.BulletSize;
        currentDame = data.Damagelv1;
        currentSpeed = data.Speedlv1;
        currentSprite = data.Spritelv1;
        currentDamageEffect = data.DamageEffectlv1;
        currentTimeEffect = data.TimeEffectlv1;
        spriteRenderer.sprite = currentSprite;
    }

    

    public virtual void Shoot()
    {
        if(target.enemiesInRange.Count >0 && target.enemiesInRange[0].gameObject != null)
        {
            GameObject bullet = pooler.GetPoolObj();
            bullet.transform.position = transform.position;
            bullet.SetActive(true);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.SetTarget(target.enemiesInRange[0].transform);
            }
           
         
        }
    }
    public void CheckLevelData(int dataDamage, float dataSpeed, Sprite sprite, float dataDamageEffect,float dataTimeEffect)
    {
        currentDame = dataDamage;
        currentSpeed = dataSpeed;
        currentSprite = sprite;
        currentDamageEffect = dataDamageEffect;
        currentTimeEffect = dataTimeEffect;
        spriteRenderer.sprite = currentSprite;

    }

    public bool TryUpgrade ()
    {
        if (currentlevel == maxLevel || GameManager.Instance.Gold < data.UpgradeCost)
        {
            return false;
        }
        currentlevel++;
        CheckLevelData(data.Damagelv2, data.Speedlv2 , data.Spritelv2, data.DamageEffectlv2 , data.TimeEffectlv2);
        GameManager.Instance.SpendGold(data.UpgradeCost);
        return true;
    }

  
}
