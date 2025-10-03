using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void StateEnter()
    {
        base.StateEnter();

        _player.InitiateJump();

        _player.StateMachine.ChangeState(_player.InAirState);
    }

    public override void StateExit()
    {
        base.StateExit();    
    }

    public override void StateFixedUpdate()
    {
        base.StateFixedUpdate();

        //even though we exit this state immediately, we need to account for movement in case a physics update is called before the transition is made for the sake of smooth movement

        _player.Move(_moveStats.AirAcceleration, _moveStats.AirDeceleration, InputManager.Movement);
    }

    public override void StateUpdate()
    {
        base.StateUpdate();

        if (InputManager.DashWasPressed && (_player.CanDash() || _player.CanAirDash()))
        {
            _player.StateMachine.ChangeState(_player.DashState);
        }
    }
}
