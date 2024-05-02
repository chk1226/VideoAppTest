using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace Sttplay.MediaPlayer
{

    /// <summary>
    /// Manage SCPlayerPro's singleton
    /// The user uses the SCPlayerPro scene in this scene to generate an object and mount the component
    /// The update function of this class also drives Dispatcher.WakeAll()
    /// </summary>
    public class SCMGR : MonoBehaviour
    {
        public static SCMGR Instance { get; private set; }
        public static bool IsPaused { get; private set; }
        private static List<UnitySCPlayerPro> players = new List<UnitySCPlayerPro>();
        private static List<ISCPlayerPro> cores = new List<ISCPlayerPro>();
        public static event System.Action SCEnvironmentInitilize;
        public static event System.Action SCEnvironmentTerminate;
        private static bool nextInit = false;

        private static AndroidJavaClass unityPlayer;
        private static AndroidJavaObject currentActivity;
        private static AndroidJavaObject context;
        private static AndroidJavaClass debugClass;
        private static System.IntPtr eglContext;
        private static int frameRate;

        private static SCThreadManager normalThreadMgr;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void UnityAutoInit()
        {
#if UNITY_EDITOR && (UNITY_2019_4_OR_NEWER)
            //EditorApplication.wantsToQuit -= UnityEditorQuit;
            //EditorApplication.wantsToQuit += UnityEditorQuit;
#endif
        }

        /// <summary>
        /// Initialize SCPlayerPro and add the player to the players list
        /// </summary>
        /// <param name="player"></param>
        public static void InitSCPlugins(UnitySCPlayerPro player)
        {
            if (Instance == null)
                new GameObject().AddComponent<SCMGR>().Awake();
            players.Add(player);
        }

        /// <summary>
        /// Get Android EGLContext
        /// </summary>
        /// <returns></returns>
        public static System.IntPtr GetEGLContext()
        {
            return eglContext;
        }

        /// <summary>
        /// Set the instance to singleton mode, and do not destroy the object 
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else if (Instance == this)
                return;
            gameObject.name = "SCMGR";
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
            unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            AndroidJavaClass jclz = new AndroidJavaClass("com.sttplay.MediaPlayer.TimeUtility");
            long creationts = jclz.CallStatic<long>("GetCreationTime", GetUrlFromSCSCAssets(""));
            if(jclz.CallStatic<long>("GetPackageLastUpdateTime", currentActivity) > creationts)
            {
                if(System.IO.Directory.Exists(GetUrlFromSCSCAssets("")))
                    System.IO.Directory.Delete(GetUrlFromSCSCAssets(""), true);
                AndroidJavaObject assetManager = currentActivity.Call<AndroidJavaObject>("getAssets");
                Debug.Log($"[_unity] persistentDataPath {Application.persistentDataPath}"); 
                new AndroidJavaClass("com.sttplay.MediaPlayer.FileUtility").CallStatic("CopyAssets", "SCAssets", Application.persistentDataPath + "/", assetManager);
            }
            GL.IssuePluginEvent(XRendererEx.XRendererEx_GetUnityRenderEventFuncPointer(), 0xfff0);
            while (XRendererEx.XRendererEx_GetUnityContext(ref eglContext) < 0)
                Thread.Sleep(1);
            int jniVer = 0;
            System.IntPtr jvm = ISCNative.GetJavaVM(ref jniVer);
            XRendererEx.XRendererEx_SetJavaVM(jvm, jniVer);
#endif
            frameRate = Screen.currentResolution.refreshRate;
            normalThreadMgr = SCThreadManager.CreateThreadManager();
            InitilizeSCPlayerPro();

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnPauseModeStateChanged;
#if UNITY_EDITOR && (UNITY_2019_4_OR_NEWER)
            CompilationPipeline.compilationStarted += OnBeginCompileScripts;
#endif //UNITY_VERSION

#endif
        }

        public static int GetMaxDisplayFrequency()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return ISCNative.GetMaxDisplayFrequency();
#elif UNITY_ANDROID
            return frameRate;
#else
            return 0
#endif
        }

#if UNITY_EDITOR && (UNITY_2019_4_OR_NEWER)
        //private static bool UnityEditorQuit()
        //{
        //    Instance.OnDestroy();
        //    return true;
        //}

        private void OnBeginCompileScripts(object obj)
        {
            OnDestroy();
        }
        private void OnPauseModeStateChanged(PauseState state)
        {
            IsPaused = state == PauseState.Paused ? true : false;
        }
#endif

        /// <summary>
        /// initialize scplugins
        /// </summary>
        private static void InitilizeSCPlayerPro()
        {
            try
            {
                ISCNative.SetAudioDriver((int)AudioDriverType.Auto);
                ISCNative.InitializeStreamCapturePro();
                nextInit = true;
            }
            catch (System.Exception ex)
            {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
                SCWin32.MessageBox(
                   SCWin32.GetProcessWnd(),
                   "The MICROSOFT VISUAL C++ 2015 - 2022 RUNTIME library is missing, please install it and try again.\n" + ex.ToString(),
                   "Error",
                   SCWin32.MB_ICONERROR);
#elif UNITY_ANDROID
                currentActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaClass Toast = new AndroidJavaClass("android.widget.Toast");
                    AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", ex.ToString());
                    AndroidJavaObject toast = Toast.CallStatic<AndroidJavaObject>("makeText", context, javaString, Toast.GetStatic<int>("LENGTH_LONG"));
                    toast.Call("show");
                }));
#endif
            }
        }

        /// <summary>
        /// terminate scplugins
        /// </summary>
        private static void TerminateSCPlayerPro()
        {
            if (SCEnvironmentTerminate != null)
                SCEnvironmentTerminate();
            ISCNative.TerminateStreamCapturePro();
            Instance.Update();
        }

        /// <summary>
        /// The local program will call back the function to Unity
        /// </summary>
        /// <param name="level">log level</param>
        /// <param name="msg">msg</param>
        public static void LogCallback(LogLevel level, string msg)
        {
            if (level == LogLevel.Info)
                Debug.LogFormat("<b>{0}</b>", msg);
            else if (level == LogLevel.Warning)
                Debug.LogWarningFormat("<b>{0}</b>", msg);
            else
                Debug.LogErrorFormat("<b>{0}</b>", msg);
        }

        public static void RemovePlayer(UnitySCPlayerPro player)
        {
            if (players.Contains(player))
                players.Remove(player);
        }
        /// <summary>
        /// release scplayerpro
        /// </summary>
        /// <param name="player"></param>
        private static void ReleasePlayer(UnitySCPlayerPro player)
        {
            player.ReleaseCore();
            RemovePlayer(player);
        }

        /// <summary>
        /// Drive Dispatcher 
        /// </summary>
        private void Update()
        {
            if (nextInit)
            {
                nextInit = false;
                if (SCEnvironmentInitilize != null)
                    SCEnvironmentInitilize();
            }
            int level = 0;
            System.IntPtr log = System.IntPtr.Zero;
            while ((log = ISCNative.PeekSCLog(ref level)) != System.IntPtr.Zero)
                LogCallback((LogLevel)level, Marshal.PtrToStringAnsi(log));
            foreach (var item in cores)
                item.Update();
        }

        private void FixedUpdate()
        {
            Update();
        }

        public static void AddPlayer(ISCPlayerPro player)
        {
            cores.Add(player);
        }
        public static void RemovePlayer(ISCPlayerPro player)
        {
            cores.Remove(player);
        }

        /// <summary>
        /// release all player
        /// </summary>
        private void OnDestroy()
        {
            while (players.Count > 0)
                ReleasePlayer(players[0]);
            SCPlayerProManager.ReleaseAll();
            TerminateSCPlayerPro();
            normalThreadMgr.Dispose();
#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= OnPauseModeStateChanged;
#endif
        }

        public static string GetUrlFromSCSCAssets(string url)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                return Application.streamingAssetsPath + "/SCAssets/" + url;
            else if (Application.platform == RuntimePlatform.Android)
                return Application.persistentDataPath + "/SCAssets/" + url;
            return null;
        }

        public static SCThreadHandle CreateThreadHandle(System.Action action)
        {
            return normalThreadMgr.CreateThreadHandle(action);
        }
        public static void ReleaseThreadHandle(SCThreadHandle handle)
        {
            if(handle != null)
                normalThreadMgr.FreeThreadHandle(handle);
        }

        public static string GetMemoryInfo()
        {
            string info = "";
            if (Application.platform == RuntimePlatform.Android)
            {
                if(debugClass == null)
                    debugClass = new AndroidJavaClass("android.os.Debug");

                long maxNativeHeapSize = debugClass.CallStatic<long>("getNativeHeapSize") / 1000000;

                long allocatedNativeHeapSize = debugClass.CallStatic<long>("getNativeHeapAllocatedSize") / 1000000;

                long freeNativeHeapSize = debugClass.CallStatic<long>("getNativeHeapFreeSize") / 1000000;

                info += $"Max Native Heap Size: {maxNativeHeapSize} MB\n";
                info += $"Allocated Native Heap Size: {allocatedNativeHeapSize} MB\n";
                info += $"Free Native Heap Size: {freeNativeHeapSize} MB";
            }
            else
            {
                //Debug.LogError("This code is intended for Android platform only.");
            }
            return info;
        }
    }
}