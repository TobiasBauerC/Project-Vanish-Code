using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleToSearch : Transition
{
    public IdleToSearch()
    {
        _baseState = State.Name.Idle;
        _targetState = State.Name.Search;
    }

    public override State GetNextState()
    {
        if (agent.currentState.name != _baseState)
        {
            return null;
        }

        bool shouldSearch = agent.LastKnownPlayerPositionReached();
        bool enemyHit = agent.stateManager.GetCondition(Condition.Name.EnemyHit).isTrue;

        if (shouldSearch && !enemyHit)
        {
            return agent.stateManager.GetState(_targetState);
        }

        return null;
    }
}