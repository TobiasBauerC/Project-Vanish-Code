using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinishedSearchingCondition : Condition
{
    protected override void AdditionalInit()
    {
        _name = Name.FinishedSearching;
        _isTrue = false;

        agent.finishedSearching += FinishedSearching;
        agent.startedSearching += StartedSearching;
    }

    private void FinishedSearching()
    {
        _isTrue = true;
    }

    private void StartedSearching()
    {
        _isTrue = false;
    }

    protected override void AdditionalShutDown()
    {
        agent.finishedSearching -= FinishedSearching;
        agent.startedSearching -= StartedSearching;
    }
}
