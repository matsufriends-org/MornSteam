// ref :https://github.com/rlabrecque/Steamworks.NET-SteamManager
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace MornSteam
{
    [DisallowMultipleComponent]
    public sealed class MornSteamManager : MonoBehaviour
    {
#if !DISABLESTEAMWORKS
        private bool _isInitialized;
        private static bool _everInitializeSucceed;
        private static MornSteamManager _instance;
        private static MornSteamManager Instance
        {
            get
            {
                if (!Application.isPlaying)
                {
                    Debug.LogWarning("[Steamworks.NET] MornSteamManagerはプレイ中のみ有効です。");
                    return null;
                }

                if (_instance == null)
                {
                    return new GameObject(nameof(MornSteamManager)).AddComponent<MornSteamManager>();
                }

                return _instance;
            }
        }
        public static bool Initialized => Instance?._isInitialized ?? false;
        public static string UserId => SteamUser.GetSteamID().ToString();
        private SteamAPIWarningMessageHook_t _steamAPIWarningMessageHook;

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        private static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
        {
            Debug.LogWarning(pchDebugText);
        }

        public static void ResetStaticField()
        {
            _everInitializeSucceed = false;
            _instance = null;
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_instance != null)
            {
                if (_instance != this)
                {
                    Destroy(gameObject);
                }

                return;
            }

            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            if (_everInitializeSucceed)
            {
                Debug.Log("[Steamworks.NET] 初期化済みなのでスキップ");
                return;
            }

            if (!Packsize.Test())
            {
                Debug.LogError("[Steamworks.NET] パックサイズテスト失敗。", this);
            }

            if (!DllCheck.Test())
            {
                Debug.LogError("[Steamworks.NET] DLLチェックテスト失敗。", this);
            }

            try
            {
                if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
                {
                    Debug.LogError("[Steamworks.NET] RestartAppIfNecessaryのため再起動。");
                    Application.Quit();
                    return;
                }
            }
            catch (System.DllNotFoundException e)
            {
                Debug.LogError("[Steamworks.NET] dllロード失敗。\n" + e, this);
                Application.Quit();
                return;
            }

            _isInitialized = SteamAPI.Init();
            if (!_isInitialized)
            {
                Debug.LogError("[Steamworks.NET] SteamAPI_Init()失敗。", this);
                return;
            }

            _everInitializeSucceed = true;
            Debug.Log("[Steamworks.NET] SteamAPI_Init()成功。", this);
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_steamAPIWarningMessageHook == null)
            {
                _steamAPIWarningMessageHook = SteamAPIDebugTextHook;
                SteamClient.SetWarningMessageHook(_steamAPIWarningMessageHook);
            }
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _instance = null;
            if (!_isInitialized)
            {
                return;
            }

            SteamAPI.Shutdown();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            SteamAPI.RunCallbacks();
        }

        private static InputHandle_t[] _mInputHandles;
        public static List<string> GetInputs()
        {
            if (_mInputHandles == null)
            {
                _mInputHandles = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
            }
            
            var result = new List<string>();
            SteamInput.GetConnectedControllers(_mInputHandles);
            foreach (var handle in _mInputHandles)
            {
                var inputType = SteamInput.GetInputTypeForHandle(handle);
                if (inputType != ESteamInputType.k_ESteamInputType_Unknown)
                {
                    result.Add(inputType.ToString());
                }
            }
            
            return result;
        }

        public static bool TryGetAchievement(string label, out bool isUnlocked)
        {
            if (Initialized)
            {
                return SteamUserStats.GetAchievement(label, out isUnlocked);
            }

            isUnlocked = false;
            return false;
        }

        public static void SetAchievement(string label)
        {
            if (Initialized)
            {
                if (SteamUserStats.RequestCurrentStats())
                {
                    SteamUserStats.SetAchievement(label);
                    SteamUserStats.StoreStats();
                }
            }
        }

        public static void SetStat(string label, int value)
        {
            if (Initialized)
            {
                if (SteamUserStats.RequestCurrentStats())
                {
                    SteamUserStats.SetAchievement(label);
                    SteamUserStats.StoreStats();
                }
            }
        }
#else
        public static bool Initialized => false;
        public static CSteamID GetUserId => CSteamID.Nil.ToString();
        public static List<string> GetInputs()
        {
            var result = new List<string>();
            return result;
        }
        public static bool TryGetAchievement(string label, out bool isUnlocked)
        {
            isUnlocked = false;
            return false;
        }
        public static void SetAchievement(string label)
        {
        }
        public static void SetStat(string label, int value)
        {
        }
#endif
    }
}