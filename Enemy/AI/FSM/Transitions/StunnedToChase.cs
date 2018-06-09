using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StunnedToChase : Transition
{
    public StunnedToChase()
    {
        _baseState = State.Name.Stunned;
        _targetState = State.Name.Chase;
    }

    public override State GetNextState()
    {
        if (agent.currentState.name != _baseState)
        {
            return null;
        }

        bool seenPlayer = agent.SeenPlayer();
        bool recover = agent.stateManager.GetCondition(Condition.Name.EnemyRecover).isTrue;

        if (recover && seenPlayer)
        {
            return agent.stateManager.GetState(_targetState);
        }

        return null;
    }
}
