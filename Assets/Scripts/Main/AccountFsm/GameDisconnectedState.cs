using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameDisconnectedState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly MixedManager _game;

    public GameDisconnectedState(ConnectionStateMachine owner, MixedManager game) : base(owner)
    {
        _game = game;
    }

    private bool isFirst = false;
    public override void Enter()
    {
        isFirst = false;
    }

    public override void Tick()
    {
        if (isFirst)
        {
            isFirst = false;
            MixedManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.ROOM);
        }
    }

    public override void Exit(bool disabled)
    {
        UIManager.Instance.EndConnecting();
    }
}
