namespace F8Framework.Core
{
    // ReSharper disable once InconsistentNaming
    public static partial class FF8
    {
        //相当于重命名
        /* ------------------------核心模块------------------------ */
        
        // 全局消息
        private static MessageManager _message;
        // 输入管理-->使用了消息模块
        private static InputManager _inputManager;
        // 本地存储
        private static StorageManager _storage;
        // 游戏时间管理-->使用了消息模块
        private static TimerManager _timer;
        // 流程管理
        private static ProcedureManager _procedure;
        // 网络管理
        private static NetworkManager _networkManager;
        // 有限状态机
        private static FSMManager _fsm;
        // 游戏对象池
        private static GameObjectPool _gameObjectPool;
        // 资产管理
        private static AssetManager _asset;
        // 读取配置表-->使用了资产模块
        private static F8DataManager _config;
        // 音频管理-->使用了资产模块
        private static AudioManager _audio;
        // 补间动画
        private static Tween _tween;
        // UI界面管理-->使用了资产模块
        private static UIManager _ui;
        // 本地化-->使用了配置模块-->使用了资产模块
        private static Localization _localization;
        // SDK管理-->使用了消息模块
        private static SDKManager _sdkManager;
        // 下载管理器
        private static DownloadManager _downloadManager;
        // 日志助手
        private static F8LogWriter _logWriter;
        
        
        /* ------------------------可选模块------------------------ */
        // 热更新版本管理
        private static HotUpdateManager _hotUpdateManager;

        #region Mono

        public static MessageManager Message
        {
            get
            {
                if (!_message)
                    _message = ModuleCenter.CreateModule<MessageManager>();
                return _message;
            }
            set
            {
                if (!_message)
                    _message = value;
            }
        }
        
        public static AudioManager Audio
        {
            get
            {
                if (!_audio)
                    _audio = ModuleCenter.CreateModule<AudioManager>();
                return _audio;
            }
            set
            {
                if (!_audio)
                    _audio = value;
            }
        }
        
        public static SDKManager SDK
        {
            get
            {
                if (!_sdkManager)
                    _sdkManager = ModuleCenter.CreateModule<SDKManager>();
                return _sdkManager;
            }
            set
            {
                if (!_sdkManager)
                    _sdkManager = value;
            }
        }
        
        public static UIManager UI
        {
            get
            {
                if (!_ui)
                    _ui = ModuleCenter.CreateModule<UIManager>();
                return _ui;
            }
            set
            {
                if (!_ui)
                    _ui = value;
            }
        }

        #endregion

        public static GameObjectPool GameObjectPool
        {
            get
            {
                if (_gameObjectPool != null) 
                    return _gameObjectPool;
                
                _gameObjectPool = ModuleCenter.CreateModule<GameObjectPool>();
                ModuleCenter.CreateModule<F8PoolGlobal>();
                return _gameObjectPool;
            }
            set
            {
                if (_gameObjectPool != null) 
                    return;
                
                _gameObjectPool = value;
                ModuleCenter.CreateModule<F8PoolGlobal>();
            }
        }
        
        public static InputManager Input
        {
            get => _inputManager ??= ModuleCenter.CreateModule<InputManager>(new DefaultInputHelper());
            set => _inputManager ??= value;
        }
        
        public static StorageManager Storage
        {
            get => _storage ??= ModuleCenter.CreateModule<StorageManager>();
            set => _storage ??= value;
        }

        public static TimerManager Timer
        {
            get => _timer ??= ModuleCenter.CreateModule<TimerManager>();
            set => _timer ??= value;
        }

        public static ProcedureManager Procedure
        {
            get => _procedure ??= ModuleCenter.CreateModule<ProcedureManager>();
            set => _procedure ??= value;
        }

        public static NetworkManager Network
        {
            get => _networkManager ??= ModuleCenter.CreateModule<NetworkManager>();
            set => _networkManager ??= value;
        }
        
        public static FSMManager FSM
        {
            get => _fsm ??= ModuleCenter.CreateModule<FSMManager>();
            set => _fsm ??= value;
        }

        public static AssetManager Asset
        {
            get => _asset ??= ModuleCenter.CreateModule<AssetManager>();
            set => _asset ??= value;
        }
        
        public static F8DataManager Config
        {
            get => _config ??= ModuleCenter.CreateModule<F8DataManager>();
            set => _config ??= value;
        }

        public static Tween Tween
        {
            get => _tween ??= ModuleCenter.CreateModule<Tween>();
            set => _tween ??= value;
        }

        
        public static Localization Local
        {
            get => _localization ??= ModuleCenter.CreateModule<Localization>();
            set => _localization ??= value;
        }
        
        public static DownloadManager Download
        {
            get => _downloadManager ??= ModuleCenter.CreateModule<DownloadManager>();
            set => _downloadManager ??= value;
        }
        
        public static F8LogWriter LogWriter
        {
            get => _logWriter ??= ModuleCenter.CreateModule<F8LogWriter>();
            set => _logWriter ??= value;
        }
        
        public static HotUpdateManager HotUpdate
        {
            get => _hotUpdateManager ??= ModuleCenter.CreateModule<HotUpdateManager>();
            set => _hotUpdateManager ??= value;
        }
    }
}

