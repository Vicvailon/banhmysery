using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void StateEnter()
    {
        base.StateEnter();

        _player.Anim.SetBool(Player.IS_WALKING, true);
    }

    public override void StateExit()
    {
        base.StateExit();
    }

    public override void StateUpdate()
    {
        base.StateUpdate();

        if (Mathf.Abs(InputManager.Movement.x) > _moveStats.MoveThreshold && InputManager.RunIsHeld)
        {
            _player.StateMachine.ChangeState(_player.RunState);
        }

        else if (Mathf.Abs(InputManager.Movement.x) < _moveStats.MoveThreshold)
        {
            _player.Anim.SetBool(Player.IS_WALKING, false);

            _player.StateMachine.ChangeState(_player.IdleState);
        }

        else if (InputManager.JumpWasPressed)
        {
            if (_player.CanJump())
            {
                _player.SpawnJumpParticles(_player.JumpParticles);

                _player.StateMachine.ChangeState(_player.JumpState);
            }
        }

        else if (_player.JumpBufferedOrCoyoteTimed())
        {
            _player.SpawnJumpParticles(_player.JumpParticles);

            _player.StateMachine.ChangeState(_player.JumpState);
        }

        if (InputManager.DashWasPressed && (_player.CanDash() || _player.CanAirDash()))
        {
            _player.StateMachine.ChangeState(_player.DashState);
        }
    }

    public override void StateFixedUpdate()
    {
        base.StateFixedUpdate();

        //this gets called here for acceleration/movement
        _player.Move(_moveStats.GroundAcceleration, _moveStats.GroundDeceleration, InputManager.Movement);
    }
}
