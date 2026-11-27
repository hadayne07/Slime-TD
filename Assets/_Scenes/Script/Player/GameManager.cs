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
Slime-TD/Assets/_Scenes/Script/Player
/

in
test/telemetry-event-log

Edit

Preview
Indent mode

Indent size

Line wrap mode

Editing GameManager.cs file contents
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
54
55
56
57
58
59
60
61
62
63
64
65
66
67
68
69
70
71
72
73
74
75
76
77
78
79
80
81
82
83
84
85
86
87
88
89
90
91
92
93
94
95
96
97
98
99
100
101
102
103
104
105
106
107
108
109
110
111
112
113
114
115
116
117
118
119
120
121
122
123
124
125
126
127
128
129
130
131
132
133
134
135
136
137
138
139
140
141
142
143
﻿using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static Action<int> CurrentHp;
    public static Action<int> CurrentGold;
    public static Action<int, int> OnPlayerHit;

    [Header("Player")]
    [SerializeField] private int hp = 100;

    [Min(0)]
    [SerializeField] protected int gold = 100;

    [Header("Game Speed")]
    [SerializeField] private float gamespeed = 1f;

    public int Hp => hp;
    // HP khởi đầu được dùng làm HP tối đa trong prototype hiện tại (chưa có hồi máu).
    public int StartingHp { get; private set; }
    public int Gold => gold;
    public float GameSpeed => gamespeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        hp = Mathf.Max(0, hp);
        StartingHp = hp;
        gold = Mathf.Max(0, gold);

        SetTimeScale(gamespeed);
    }

    private void OnEnable()
    {
        if (Instance != this) return;

        EnemyMove.OnEnemyLeaked += HandleEnemyLeaked;
        EnemyStatus.goldplus += HandleEnemyDestroyed;
    }

    private void OnDisable()
    {
        EnemyMove.OnEnemyLeaked -= HandleEnemyLeaked;
        EnemyStatus.goldplus -= HandleEnemyDestroyed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        CurrentHp?.Invoke(hp);
        CurrentGold?.Invoke(gold);
    }

    private void HandleEnemyLeaked(EnemyData data, EnemyType enemyType)
    {
        if (data == null || hp <= 0) return;

        int hpBefore = hp;
        hp = Mathf.Max(0, hp - data.Damage);

        TelemetryEventLogger.RecordEnemyLeak(enemyType, data.Damage, hpBefore - hp, hp);
        OnPlayerHit?.Invoke(data.Damage, hp);
        CurrentHp?.Invoke(hp);

        if (hp <= 0)
        {
            TelemetryEventLogger.RecordDeath();
        }
    }

    private void HandleEnemyDestroyed(EnemyData data)
    {
        if (data == null) return;

        AddGold(data.GoldFall);
    }

    public void AddGold(int amount)
    {
        if (amount < 0)
        {
            return;
        }

        gold += amount;

        CurrentGold?.Invoke(gold);
    }

    public bool TrySpendGold(int cost)
    {
        if (cost < 0)
        {
            return false;
        }

        if (gold < cost)
        {
            return false;
        }

        gold -= cost;

        CurrentGold?.Invoke(gold);

        return true;
    }

    public void SpendGold(int cost)
    {
        TrySpendGold(cost);
    }

    public void SetTimeScale(float scale)
    {
        Time.timeScale = Mathf.Max(0f, scale);
    }

    public void SetGameSpeed(float newSpeed)
    {
        gamespeed = Mathf.Max(0f, newSpeed);

        SetTimeScale(gamespeed);
    }
}

Use Control + Shift + m to toggle the tab key moving focus. Alternatively, use esc then tab to move to the next interactive element on the page.
