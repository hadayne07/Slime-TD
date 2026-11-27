hadayne07
Slime-TD
Repository navigation
Code
Issues
Pull requests
Agents
Actions
Projects
Wiki
Security and quality
Insights
Settings
Slime-TD/Assets/_Scenes/Script/Enemy
/

in
test/telemetry-event-log

Edit

Preview
Indent mode

Indent size

Line wrap mode

Editing EnemyMove.cs file contents
Loading editor
1
2
3
4
5
6
7
8
9
10
11
12
13
14
15
16
17
18
19
20
21
22
23
24
25
26
27
28
29
30
31
32
33
34
35
36
37
38
39
40
41
42
43
44
45
46
47
48
49
50
51
52
53
﻿using UnityEngine;
using System;
public class EnemyMove : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] protected Path currentpath;
    private EnemyStatus enemyStatus;
    public static event Action<EnemyData> OnEnemyReachEnd;
    public static event Action<EnemyData, EnemyType> OnEnemyLeaked;
    
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
                OnEnemyLeaked?.Invoke(data, enemyStatus.Type);
                OnEnemyReachEnd?.Invoke(data);
                gameObject.SetActive(false);
            }
        }
    }

   
}

Use Control + Shift + m to toggle the tab key moving focus. Alternatively, use esc then tab to move to the next interactive element on the page.
