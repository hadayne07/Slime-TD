using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI; 

public class TowerCard : MonoBehaviour
{
    [SerializeField] private Image towerImage; 
    [SerializeField] private TMP_Text cost;
    private TowerData towerData;
    public static Action<TowerData> OnTowerSelected;
    
    public void Initialize(TowerData data)
        
    {
        towerData = data;
        towerImage.sprite = data.Sprite;
        cost.text = data.Cost.ToString();
    
    }

    public void PlaceTower()
    {
        OnTowerSelected?.Invoke(towerData);
      
    }
}
