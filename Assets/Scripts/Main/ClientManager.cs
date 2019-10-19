using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Main
{
    public class ClientManager : MonoBehaviour
    {
        // 状态机
        private ConnectionStateMachine _stateMachine;
        public ConnectionStateMachine StateMachine => _stateMachine;

        // 客户端网络链接-大厅
        [SerializeField] private GameLobbyManager _lobbyManager;
        public GameLobbyManager LobbyManager => _lobbyManager;

        public static ClientManager Instance { private set; get; }

        void Awake()
        {
            Instance = this;
            _lobbyManager.gameObject.SetActive(false);
            DontDestroyOnLoad(gameObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            _stateMachine = new ConnectionStateMachine(this);
            _stateMachine.OnEnable(ConnectionFSMStateEnum.StateEnum.START);
        }

        private void OnDestroy()
        {
            _stateMachine.OnDisable();
        }

        // Update is called once per frame
        void Update()
        {
            _stateMachine.Tick();
        }
    }
}
