public class SearchToChase : Transition
{
    public SearchToChase()
    {
        _baseState = State.Name.Search;
        _targetState = State.Name.Chase;
    }

    public override State GetNextState()
    {
        if (agent.currentState.name != _baseState)
        {
            return null;
        }

        bool playerDetected = agent.stateManager.GetCondition(Condition.Name.PlayerDetected).isTrue;
        //bool playerReachable = agent.stateManager.GetCondition(Condition.Name.PlayerReachable).isTrue;
        bool enemyHit = agent.stateManager.GetCondition(Condition.Name.EnemyHit).isTrue;
        bool lppReached = agent.LastKnownPlayerPositionReached();

        if (!enemyHit && playerDetected)
        {
            return agent.stateManager.GetState(_targetState);
        }

        return null;
    }
}
