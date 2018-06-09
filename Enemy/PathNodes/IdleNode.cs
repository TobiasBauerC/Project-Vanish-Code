using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleNode : BaseNode
{
    [Range(0.0f, 3600.0f)]
    [SerializeField]
    private float _idleTime = 1.0f;

    void Start()
    {
        // set up node ID
        Name = BaseNode.Type.Idle;
    }

    public float GetIdleTime()
    {
        return _idleTime;
    }
}
