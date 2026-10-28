using System;
using UnityEngine;

[Serializable]
public class EnemyGroup
{
    public EnemyType enemyType;

    [Min(1)]
    public int count = 1;

    [Min(0f)]
    public float startDelay = 0f;

    [Min(0.01f)]
    public float spawnInterval = 1f;

}