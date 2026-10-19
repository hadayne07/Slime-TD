using UnityEngine;
using System;
public class EnemyMove : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] protected Path currentpath;
    private EnemyStatus enemyStatus;
    public static event Action<EnemyData> OnEnemyReachEnd;
    
    protected Vector3 targetWaypoint;
    protected int currentPosition;
    protected float distance;

    private void Awake()
    {
        currentpath = GameObject.FindWithTag("PathWave").GetComponent<Path>();
        enemyStatus = GetComponent<EnemyStatus>();
    }
    private void OnEnable()
    {
        currentPosition = 0;
        targetWaypoint = currentpath.getPosition(currentPosition);
    }


    public void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, enemyStatus.currentSpeed * Time.deltaTime);


        distance = (transform.position - targetWaypoint).magnitude;

      
        
        if ( distance <= 0.1f)
            {
     
            if (currentPosition < currentpath.Waypoints.Count-1)
            {
                currentPosition++;
                targetWaypoint = currentpath.getPosition(currentPosition);
            }
            else
            {
                OnEnemyReachEnd?.Invoke(data);
                gameObject.SetActive(false);
            }
        }
    }

   
}
