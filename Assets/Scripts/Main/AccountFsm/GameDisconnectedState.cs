using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;
using UnityEditor.PackageManager;

public class GameDisconnectedState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    public GameDisconnectedState(ConnectionStateMachine owner, ClientManager game) : base(owner)
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
            ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.ROOM);
        }
    }

    public override void Exit(bool disabled)
    {
        UIManager.Instance.EndConnecting();
    }
}
