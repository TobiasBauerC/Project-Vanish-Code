using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReachableCondition : Condition
{
    protected override void AdditionalInit()
    {
        _name = Name.PlayerReachable;
        _isTrue = true;

        agent.playerReachable += PlayerReachable;
        agent.playerNotReachable += PlayerNotReachable;
    }

    private void PlayerReachable()
    {
        _isTrue = true;
    }

    private void PlayerNotReachable()
    {
        _isTrue = false;
    }

    protected override void AdditionalShutDown()
    {
        agent.playerReachable -= PlayerReachable;
        agent.playerNotReachable -= PlayerNotReachable;
    }
}
