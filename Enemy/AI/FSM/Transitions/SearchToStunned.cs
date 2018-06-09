public class SearchToStunned : Transition
{
    public SearchToStunned()
    {
        _baseState = State.Name.Search;
        _targetState = State.Name.Stunned;
    }

    public override State GetNextState()
    {
        if (agent.currentState.name != _baseState)
        {
            return null;
        }

        bool enemyHit = agent.stateManager.GetCondition(Condition.Name.EnemyHit).isTrue;

        if (enemyHit)
        {
            return agent.stateManager.GetState(_targetState);
        }

        return null;
    }
}
