using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{
    protected Player _player;
    protected PlayerStateMachine _playerStateMachine;

    protected PlayerMovementStats _moveStats;

    protected bool _isExitingState;

    public PlayerState(Player player, PlayerStateMachine stateMachine)
    {
        _player = player;
        _playerStateMachine = stateMachine;

        _moveStats = _player.MoveStats;
    }

    //STATE LOGIC FUNCTIONS -- (add any functions you may want to use here. For example, if you want an OnCollisionEnter2D function, add a custom one here, then call that custom function from OnCollisionEnter2D in the Player script.)
    //I've kept this basic since we don't need much for this little demo

    /// <summary>
    /// Called as soon as you enter a state
    /// </summary>
    public virtual void StateEnter()
    {
        _isExitingState = false;

        if (_player.ShowEnteredStateDebugLog)
        Debug.Log("Entered State: " + _playerStateMachine.CurrentState);
    }

    /// <summary>
    /// Called as soon as you exit a state
    /// </summary>
    public virtual void StateExit()
    {
        _isExitingState = true;
    }

    /// <summary>
    /// Called every frame
    /// </summary>
    public virtual void StateUpdate()
    {
        //call the timers here
        _player.JumpTimers();
        _player.WallJumpTimers();
        _player.DashTimers();
        _player.JumpInputChecks();
        _player.WallJumpChecks();

        //handle falling (might happen with a low enough deceleration after movement stops)
        _player.CheckForFalling();
    }

    /// <summary>
    /// Called every physics time step (0.02f, ie 50x per second by default)
    /// </summary>
    public virtual void StateFixedUpdate()
    {
        _player.CollisionChecks();

        _player.ApplyVelocity();
    }



}
