using UnityEngine;

public class EnemyCtrl : MonoBehaviour
{
    
    [SerializeField] EnemyMove enemyMove;
    private void Start()
    {
        enemyMove = GetComponent<EnemyMove>();
   
    }

    private void Update()
    { 
            enemyMove.Move();
       
    }
}
