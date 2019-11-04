using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameConnectedState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly MixedManager _game;

    public GameConnectedState(ConnectionStateMachine owner, MixedManager game) : base(owner)
    {
        _game = game;
    }

    private bool _bFirst;
    public override void Enter()
    {
        _bFirst = true;
    }

    public override void Tick()
    {
        if (_bFirst)
        { // 只运行一帧，就切换到下个状态了
            MixedManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.ROOM);
            _bFirst = false;
        }
    }

    public override void Exit(bool disabled)
    {
    }
}
