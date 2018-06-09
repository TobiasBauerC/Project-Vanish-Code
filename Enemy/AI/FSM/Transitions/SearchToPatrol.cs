public class SearchToPatrol : Transition
{
    public SearchToPatrol()
    {
        _baseState = State.Name.Search;
        _targetState = State.Name.Patrol;
    }

    public override State GetNextState()
    {
        if (agent.currentState.name != _baseState)
        {
            return null;
        }

        bool playerDetected = agent.stateManager.GetCondition(Condition.Name.PlayerDetected).isTrue;
        bool finishedSearching = agent.stateManager.GetCondition(Condition.Name.FinishedSearching).isTrue;
        bool enemyHit = agent.stateManager.GetCondition(Condition.Name.EnemyHit).isTrue;

        if (!playerDetected && finishedSearching && !enemyHit)
        {
            return agent.stateManager.GetState(_targetState);
        }

        return null;
    }
}
