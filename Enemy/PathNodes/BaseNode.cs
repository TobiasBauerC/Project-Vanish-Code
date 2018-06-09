using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseNode : MonoBehaviour
{
    public enum Type
    {
        Patrol,
        Idle,
        Search
    }

    protected Type Name;
    public Type GetName()
    {
        return Name;
    }

    void Awake()
    {
        // give base node a default of Patrol
        Name = Type.Patrol;
    }
}