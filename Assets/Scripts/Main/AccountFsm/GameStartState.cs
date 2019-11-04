using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

public class GameStartState : FsmBaseState<ConnectionStateMachine, ConnectionFSMStateEnum.StateEnum>
{
    private readonly MixedManager _game;

    private GameObject _panelLogin;

    public GameStartState(ConnectionStateMachine owner, MixedManager game) : base(owner)
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
