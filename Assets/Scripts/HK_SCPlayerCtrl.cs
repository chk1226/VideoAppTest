using Sttplay.MediaPlayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using TMPro;

public class HK_SCPlayerCtrl : MonoBehaviour
{
    public string URL;
    public UnitySCPlayerPro SCPlayer = null;
    public TextMeshProUGUI TextComp = null;

    protected float RegFPS = 0.0f;
    protected float FPS = 0.0f;
    protected int FPSCount = 0;
    protected FrameTiming[] m_FrameTimings = new FrameTiming[15];

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
    private static AndroidJavaClass unityPlayer;
    private static AndroidJavaObject currentActivity;
    private static AndroidJavaObject context;
    private static System.IntPtr eglContext;

#endif
    // Start is called before the first frame update
    void Start()
    {
        SCPlayer.onFirstFrameRenderEvent.AddListener(FirstVideoFrameRender);
        Open();
        Play();
    }

    private void OnDestroy()
    {
        //Close();
    }

    private void Awake()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
#elif UNITY_ANDROID
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
        AndroidJavaClass jclz = new AndroidJavaClass("com.sttplay.MediaPlayer.TimeUtility");
        long creationts = jclz.CallStatic<long>("GetCreationTime", GetBasePath());
        if(jclz.CallStatic<long>("GetPackageLastUpdateTime", currentActivity) > creationts)
        {
            if(System.IO.Directory.Exists(GetBasePath()))
                System.IO.Directory.Delete(GetBasePath(), true);
            AndroidJavaObject assetManager = currentActivity.Call<AndroidJavaObject>("getAssets");
            Debug.Log($"[_unity] persistentDataPath {Application.persistentDataPath}"); 
            new AndroidJavaClass("com.sttplay.MediaPlayer.FileUtility").CallStatic("CopyAssets", "Video", Application.persistentDataPath + "/", assetManager);
        }
        GL.IssuePluginEvent(XRendererEx.XRendererEx_GetUnityRenderEventFuncPointer(), 0xfff0);
        while (XRendererEx.XRendererEx_GetUnityContext(ref eglContext) < 0)
            Thread.Sleep(1);
        int jniVer = 0;
        System.IntPtr jvm = ISCNative.GetJavaVM(ref jniVer);
        XRendererEx.XRendererEx_SetJavaVM(jvm, jniVer);
#endif
    }

    protected void FirstVideoFrameRender(SCRenderer render)
    {
        Debug.Log($"[_unity] video w({render.SyntheticTexture.width}) h({render.SyntheticTexture.height})"); 

    }
    protected string GetBasePath()
    {
        string _base = "";
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            _base = Application.streamingAssetsPath + "/Video";
        else if (Application.platform == RuntimePlatform.Android)
            _base = Application.persistentDataPath + "/Video";
        return _base;
    }
    public string GetFilePath()
    {
        string videoPath = "";

        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            videoPath = GetBasePath() + "/" + URL + ".mp4";
        else if (Application.platform == RuntimePlatform.Android)
            videoPath = GetBasePath() + "/" + URL + ".mp4";
        return videoPath;
    }
    public void Open()
    {
        var videoUrl = GetFilePath();
        Debug.Log($"[_unity] {videoUrl}");

        SCPlayer.Open(MediaType.LocalFile, videoUrl);

        var render = SCPlayer.VideoRenderer;


    }

    public void Pause() 
    {
        if(!SCPlayer.IsPaused)
        {
            SCPlayer.Pause();
        }
    }
    public void Close()
    {
        if(!SCPlayer.Closed)
            SCPlayer.Close();
    }

    public void Play()
    {
        if(SCPlayer.Closed)
        {
            Open();
        }
        SCPlayer.Play();
    }



    // Update is called once per frame
    void Update()
    {
        if(FPSCount == 10)
        {
            FPS = RegFPS / FPSCount;
            RegFPS = 0.0f;
            FPSCount = 0;
        }
        else
        {
            RegFPS += 1.0f / Time.smoothDeltaTime;
            FPSCount++;
        }

        // Instruct FrameTimingManager to collect and cache information
        FrameTimingManager.CaptureFrameTimings();


        // Read cached information about N last frames (10 in this example)
        // The returned value tells how many samples is actually returned
        var ret = FrameTimingManager.GetLatestTimings((uint)m_FrameTimings.Length, m_FrameTimings);


        if (TextComp)
        {
            string text = string.Format($"FPS : {FPS:F2}\n");
            if(ret > 0)
            {
                float cpuFrameTime = 0;
                float cpuMainFrameTime = 0;
                float gpuFrameTime = 0;
                for (int i = 0; i < m_FrameTimings.Length; i++)
                {
                    cpuFrameTime += (float)m_FrameTimings[i].cpuFrameTime;
                    cpuMainFrameTime += (float)m_FrameTimings[i].cpuMainThreadFrameTime;
                    gpuFrameTime += (float)m_FrameTimings[i].gpuFrameTime;
                }
                text += string.Format($"CPU: {cpuFrameTime / m_FrameTimings.Length :F2}ms\n");
                text += string.Format($"CPU Main: {cpuMainFrameTime / m_FrameTimings.Length:F2}ms\n");
                text += string.Format($"GPU: {gpuFrameTime / m_FrameTimings.Length:F2}ms\n");
            }

            TextComp.SetText(text);
        }
    }
}
