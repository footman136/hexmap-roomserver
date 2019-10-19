using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameStartState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly ClientManager _game;

    private GameObject _panelLogin;

    public GameStartState(ConnectionStateMachine owner, ClientManager game) : base(owner)
    {
        _game = game;
    }

    public override void Enter()
    {
        UIManager.Instance.EndLoading();
    }

    public override void Tick()
    {
    }

    public override void Exit(bool disabled)
    {
        if (_panelLogin != null)
        {
            UIManager.DestroyPanel(ref _panelLogin);
        }
    }
}
