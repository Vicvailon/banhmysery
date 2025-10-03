using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInAirState : PlayerState
{
    public PlayerInAirState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void StateEnter()
    {
        base.StateEnter();
    }

    public override void StateExit()
    {
        base.StateExit();
    }

    public override void StateUpdate()
    {
        base.StateUpdate();

        //other state transitions

        //JUMP/WallJump
        if (InputManager.JumpWasPressed)
        {
            if (_player.CanJump())
            {
                _player.SpawnJumpParticles(_player.JumpParticles);

                _player.StateMachine.ChangeState(_player.JumpState);
            }

            if (_player.CanAirJump())
            {
                _player.SpawnJumpParticles(_player.SecondJumpParticles);

                _player.StateMachine.ChangeState(_player.JumpState);
            }

            if (_player.CanWallJumpDueToPostBufferTimer())
            {
                _player.UseWallJumpMoveStats = true;
                _player.StateMachine.ChangeState(_player.WallJumpState);
            }
        }

        else if (_player.JumpBufferedOrCoyoteTimed())
        {
            _player.SpawnJumpParticles(_player.JumpParticles);
            _player.StateMachine.ChangeState(_player.JumpState);
        }

        //LAND
        if (_player.HasLanded())
        {
            _player.SpawnJumpParticles(_player.LandParticles);

            _player.StateMachine.ChangeState(_player.IdleState);
        }

        //WALL SLIDE
        if (_player.ShouldWallSlide())
        {
            _player.StateMachine.ChangeState(_player.WallSlideState);
        }

        //DASH
        if (InputManager.DashWasPressed && (_player.CanDash() || _player.CanAirDash()))
        {
            _player.StateMachine.ChangeState(_player.DashState);
        }
    }
    

    public override void StateFixedUpdate()
    {
        base.StateFixedUpdate();

        _player.JumpPhysics();
        _player.WallJumpPhysics();
        _player.DashPhysics();


        //movement
        if (_player.UseWallJumpMoveStats)
        {
            _player.Move(_moveStats.WallJumpMoveAcceleration, _moveStats.WallJumpMoveDeceleration, InputManager.Movement);
        }

        else
        {
            _player.Move(_moveStats.AirAcceleration, _moveStats.AirDeceleration, InputManager.Movement);     
        }
    }
}
