using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine
{
    public PlayerState CurrentState { get; private set; }

    public void InitializeDefaultState(PlayerState startState)
    {
        CurrentState = startState;
        CurrentState.StateEnter();
    }

    public void ChangeState(PlayerState newState)
    {
        CurrentState.StateExit();
        CurrentState = newState;
        CurrentState.StateEnter();
    }
}
