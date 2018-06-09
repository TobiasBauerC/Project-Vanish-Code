using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AIController))]
[RequireComponent(typeof(NavMeshAgent))]

public class AIStunned : MonoBehaviour
{
    [SerializeField] private float _maxStunTime = 0f;

    private Enemy _enemy = null;
    private float _stunTime = 0.0f;
    private float _stunElapsedTime = 0.0f;

    void Start()
    {
        if (!_enemy)
        {
            _enemy = GetComponent<Enemy>();
        }

        if (_maxStunTime == 0f)
        {
            _maxStunTime = 10f;
        }
    }

    /// <summary>
    ///  Called for entering Stunned State. Takes the player's last position, the amount of time it is to be stunned, and whether or not the player has been seen.
    /// </summary>
    public void EnterStun(float stunTime, bool rangedAttack = false)
    {
        _enemy.navMeshAgent.autoBraking = true;
        _enemy.navMeshAgent.speed = 0.0f;

        if (rangedAttack)
        {
            _stunTime += stunTime;
        }
        else // melee attack
        {
            if (stunTime > _stunTime) // if new stun time (5 sec from melee) is more than current stun time
            {
                _stunTime = stunTime;
            }
        }

        // Clamps stun time to a maximum value
        _stunTime = Mathf.Min(_maxStunTime, _stunTime);
    }

    /// <summary>
    /// Updates the stunned state. Should be called every frame update.
    /// </summary>
    public void UpdateStun()
    {
        if (!IsStunned())
        {
            _enemy.Recover();
        }
    }

    /// <summary>
    /// Exits the stunned state.
    /// </summary>
    public void ExitStun()
    {
        _stunTime = 0f;
        _stunElapsedTime = 0.0f;
        _enemy.navMeshAgent.autoBraking = false;
        _enemy.FinishRecover(); // [THISSHIT]
    }

    /// <summary>
    /// Determines whether this instance is stunned.
    /// </summary>
    /// <returns><c>true</c> if this instance is stunned; otherwise, <c>false</c>.</returns>
    private bool IsStunned()
    {
        /* while the elapsed time is less than the time it is to be stunned, it will return true and code will not move forward */
        if (_stunElapsedTime < _stunTime)
        {
            _stunElapsedTime += Time.deltaTime;
            return true;
        }
        else
        {
            return false;
        }
    }
}
