using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [SerializeField] protected List<GameObject> pool;
    [SerializeField] protected int poolSize = 7;
    [SerializeField] protected GameObject prefab;

    private void Awake()
    {
        pool = new List<GameObject>();
        for ( int i = 0; i < poolSize; i++)
        {
            CreatPoolObj();
        }
        prefab.SetActive(false);
    }

    protected GameObject CreatPoolObj()
    {
        GameObject newPrefab = Instantiate(prefab, transform);
        pool.Add(newPrefab);
        newPrefab.SetActive(false);
        return newPrefab;
    }

    public GameObject GetPoolObj()
    {
      foreach  ( GameObject obj in pool)
        {
            if (!obj.activeSelf)
            {
                return obj;
            }
            
        }
        return CreatPoolObj();
    }

}
