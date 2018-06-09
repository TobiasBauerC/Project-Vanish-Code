using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIChase : MonoBehaviour
{
    private float _elapsedTime = 0f;
    public float elapsedTime
    {
        get { return _elapsedTime; }
    }

    [SerializeField] private Enemy _enemy;
    [SerializeField] private float _chaseSpeed = 5.0f;
    [SerializeField] private float _heightThreshold = 5.0f;

    public float heightThreshold
    {
        get { return _heightThreshold; }
    }

    public bool unreachable
    {
        get;
        private set;
    }

    // TODO something something delegates something conditions something something
    private bool m_playerDetected = false;

    void Start()
    {
        if (!_enemy)
        {
            _enemy = GetComponent<Enemy>();
        }

        _enemy.playerDetected += PlayerDetected;
        _enemy.playerLost += PlayerLost;
    }

    public void EnterChase()
    {
        //_enemy.navMeshAgent.SetDestination(_enemy.target.position);
        _enemy.navMeshAgent.speed = _chaseSpeed;
        _enemy.navMeshAgent.autoBraking = true;
        _enemy.navMeshAgent.autoRepath = true;
        unreachable = false;
        _elapsedTime = 0f;

        // TODO Might break if player is not reachable on this agent's navmesh
        if (m_playerDetected)
        {
            _enemy.navMeshAgent.SetDestination(_enemy.target.position);
        }
        else
        {
            _enemy.navMeshAgent.SetDestination(_enemy.lastKnownPlayerPosition);
        }
    }

    public void UpdateChase()
    {
        _elapsedTime += Time.deltaTime;

        if (m_playerDetected)
        {
            // If current destination is more than 1 unit away from target's current position, reset destination
            if (Vector3.Distance(_enemy.navMeshAgent.destination, _enemy.target.position) > 1f)
            {
                _enemy.navMeshAgent.SetDestination(_enemy.target.position);
            }
        }
        else
        {
            // If the player is not detected and this agent's destination is NOT the player's last known position
            if (_enemy.navMeshAgent.destination != _enemy.lastKnownPlayerPosition)
            {
                _enemy.navMeshAgent.SetDestination(_enemy.lastKnownPlayerPosition);
            }
        }
    }

    public void ExitChase()
    {
        if (_elapsedTime < 0.5f)
            _enemy.ResetLastPlayerPosition();
        _elapsedTime = 0f;
    }

    public void OnDestroy()
    {
        _enemy.playerDetected -= PlayerDetected;
        _enemy.playerLost -= PlayerLost;
    }

    #region Delegate Listeners
    private void PlayerDetected()
    {
        m_playerDetected = true;
    }

    private void PlayerLost()
    {
        m_playerDetected = false;
    }
    #endregion
}