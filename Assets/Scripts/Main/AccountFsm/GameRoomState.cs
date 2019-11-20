using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameRoomState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly MixedManager _game;

    public GameRoomState(ConnectionStateMachine owner, MixedManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        UIManager.Instance.EndConnecting();
        Debug.Log("Room-Server is OK!"); // 房间服务器
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
    }
}
