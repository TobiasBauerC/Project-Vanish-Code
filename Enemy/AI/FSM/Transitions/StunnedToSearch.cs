using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunnedToSearch : Transition
{
    public StunnedToSearch()
    {
        _baseState = State.Name.Stunned;
        _targetState = State.Name.Search;
    }

    public override State GetNextState()
    {
        if (agent.currentState.name != _baseState)
        {
            return null;
        }

        bool recover = agent.stateManager.GetCondition(Condition.Name.EnemyRecover).isTrue;
        bool seenPlayer = agent.SeenPlayer();

        if (recover && !seenPlayer)
        {
            return agent.stateManager.GetState(_targetState);
        }

        return null;
    }
}
