using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Gamelogic.FSM;
using Main;

/// <summary>
/// 客户端管理游戏状态的状态机 
/// </summary>
public class ConnectionStateMachine : FiniteStateMachine<ConnectionFSMStateEnum.StateEnum>
{
    private ClientManager _game;
    public ConnectionFSMStateEnum.StateEnum CurrentState { private set; get; }
    public float _startTime; // 本状态开始的时间
    [SerializeField] private bool logChanges = true;
    
    public ConnectionStateMachine(ClientManager game)
    {
        _game = game;
            
        var startState = new GameStartState(this, _game);
        var connectingState = new GameConnectingState(this, _game);
        var connectedState = new GameConnectedState(this, _game);
        var disconnectedState = new GameDisconnectedState(this, _game);
        var roomState = new GameRoomState(this, _game);

        var stateList = new Dictionary<ConnectionFSMStateEnum.StateEnum, IFsmState>
        {
            {ConnectionFSMStateEnum.StateEnum.START, startState},
            {ConnectionFSMStateEnum.StateEnum.CONNECTING, connectingState},
            {ConnectionFSMStateEnum.StateEnum.CONNECTED, connectedState},
            {ConnectionFSMStateEnum.StateEnum.DISCONNECTED, disconnectedState},
            {ConnectionFSMStateEnum.StateEnum.ROOM, roomState},
        };
        SetStates(stateList);
        
        var allowedTransitions = new Dictionary<ConnectionFSMStateEnum.StateEnum, IList<ConnectionFSMStateEnum.StateEnum>>();

        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.START, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.CONNECTING,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.CONNECTING, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.CONNECTED,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.CONNECTED, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.ROOM,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.DISCONNECTED, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.START,
        });
        allowedTransitions.Add(ConnectionFSMStateEnum.StateEnum.ROOM, new List<ConnectionFSMStateEnum.StateEnum>
        {
            ConnectionFSMStateEnum.StateEnum.START,
        });
        SetTransitions(allowedTransitions);
    }
    public void TriggerTransition(ConnectionFSMStateEnum.StateEnum newState)
    {
        if (IsValidTransition(newState))
        {
            var oldState = CurrentState; 
            CurrentState = newState;

            _startTime = Time.time;
            TransitionTo(newState);
            if (logChanges)
            {
                Debug.Log("DinoStateMachine: State changed from<" + oldState + "> to<" + newState + ">");
            }
        }
        else
        {
            Debug.LogErrorFormat("DinoStateMachine: Invalid transition from {0} to {1} detected.",
                CurrentState, newState);
        }
    }
    protected override void OnEnableImpl()
    {
        _startTime = Time.time;
    }
    
}
