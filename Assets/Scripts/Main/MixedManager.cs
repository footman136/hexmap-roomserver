using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Main
{
    public class MixedManager : MonoBehaviour
    {
        // 状态机
        private ConnectionStateMachine _stateMachine;
        public ConnectionStateMachine StateMachine => _stateMachine;

        // 客户端网络链接-大厅
        public GameLobbyManager LobbyManager;
        // 房间服务器自身
        public ServerRoomManager RoomManager;

        // 数据表
        [HideInInspector]
        public CsvDataManager CsvDataManager;

        public static MixedManager Instance { private set; get; }

        void Awake()
        {
            if(Instance != null)
                Debug.LogError("ClientManager is Singleton! Cannot be created again!");
            Instance = this;
            LobbyManager.gameObject.SetActive(false);
            
            // 读取数据表
            CsvDataManager = gameObject.AddComponent<CsvDataManager>();
            CsvDataManager.LoadDataAll();
            
            // 限制帧速率
            Application.targetFrameRate = 30;
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
