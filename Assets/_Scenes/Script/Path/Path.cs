using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour
{
    [SerializeField] protected  List<Transform> waypoints;
    public List<Transform> Waypoints { get => waypoints; }


    public Vector3 getPosition ( int index )
    {
        return waypoints[index].position;
    }
    protected void OnDrawGizmos()
    {
        if (waypoints.Count > 0)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (i < waypoints.Count-1 )
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                   
                }    
            }
        }
            
    }

}



