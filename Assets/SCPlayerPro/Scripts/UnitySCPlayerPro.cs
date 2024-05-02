using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace Sttplay.MediaPlayer
{

    [System.Serializable]
    public class CaptureOpenCallback : UnityEvent<CaptureOpenResult, string, OpenCallbackContext> { }

    [System.Serializable]
    public class RenderFrame : UnityEvent<SCRenderer> { }

    [System.Serializable]
    public class InterruptReadMedia : UnityEvent<string> { }

    public class UnitySCPlayerPro : MonoBehaviour
    {

        /// <summary>
        /// URL When the choice of mediatype is different, url has different meanings
        /// LocalOrNetworkFile:local file, http, https
        /// </summary>
        public string url;

        /// <summary>
        /// Mark whether player is closed
        /// Open failure is also considered not close
        /// </summary>
        public bool Closed { get { return core.Closed; } }

        /// <summary>
        /// Mark whether player successfully opened media
        /// </summary>
        public bool OpenSuccessed { get { return core.OpenSuccessed; } }

        /// <summary>
        /// Whether to disable video 
        /// </summary>
        public bool disableVideo = false;
        /// <summary>
        /// Whether to disable audio
        /// </summary>
        public bool disableAudio = false;

        //public bool disableSubtitle = true;
        public int defaultVideoTrack = 0;
        public int defaultAudioTrack = 0;
        //public int defaultSubtitleTrack = 0;

        /// <summary>
        /// Whether to enable hardware acceleration
        /// Not all videos support hardware acceleration.
        /// If you enable this option, hardware acceleration will be tried first, 
        /// and if it fails, the CPU will be used for decoding. 
        /// </summary>
        public bool enableHWAccel = true;

        /// <summary>
        /// Extract frame data to memory
        /// </summary>
        public bool extractHWFrame = true;

        /// <summary>
        /// Hardware device type when video hardware accelerates decoding 
        /// Not all of the current platforms are supported, 
        /// if the current option does not support, set as the default 
        /// </summary>
        public HWDeviceType HWAccelType = HWDeviceType.AUTO;

        /// <summary>
        /// Pixel format of output SCFrame 
        /// </summary>
        public PixelFormat outputPixelFormat = PixelFormat.AUTO;

        public MediaType openMode = MediaType.LocalFile;

        public int cameraWidth = 0;
        public int cameraHeight = 0;
        public float cameraFPS = 0.0f;
        public string options;

        /// <summary>
        /// Whether to open the media when UnityPlayer starts 
        /// </summary>
        public bool autoOpen = true;

        /// <summary>
        /// Play directly after opening or stay at the first frame
        /// </summary>
        public bool openAndPlay = true;

        /// <summary>
        /// Whether the media is played in a loop 
        /// This option is valid only when the mediaType is LocalOrNetworkFile 
        /// </summary>
        public bool loop = false;
        private bool _loop;

        /// <summary>
        /// Media volume
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float volume = 0.5f;

        /// <summary>
        /// Playback speed
        /// </summary>
        [Range(0.5f, 2.0f)]
        public float speed = 1.0f;

        /// <summary>
        /// Whether the marker is in a paused state 
        /// </summary>
        public bool IsPaused { get { return core.IsPaused; } }

        /// <summary>
        /// Current playback timestamp, valid when the mediaType is LocalOrNetFile
        /// </summary>
        public long CurrentTime { get { return core.CurrentTime; } }

        /// <summary>
        /// The total duration of the media, valid when the mediaType is LocalOrNetFile 
        /// </summary>
        public long Duration { get { return core.Duration; } }


        /// <summary>
        /// Called when player demux succeeds or failed
        /// </summary>
        public CaptureOpenCallback onCaptureOpenCallbackEvent;

        /// <summary>
        /// Called when opening 
        /// </summary>
        public UnityEvent onOpenEvent;

        /// <summary>
        /// Called when closing 
        /// </summary>
        public UnityEvent onCloseEvent;

        /// <summary>
        /// Called when renderer is changed
        /// </summary>
        public RenderFrame onRendererChangedEvent;

        /// <summary>
        /// Called when the video has finished playing, whether looping or not 
        /// </summary>
        public UnityEvent onStreamFinishedEvent;

        /// <summary>
        /// Called after the first frame is drawn, if there is no video stream, this event will not be called 
        /// </summary>
        public RenderFrame onFirstFrameRenderEvent;

        /// <summary>
        /// Called when player demux read pakcet failed
        /// </summary>
        public InterruptReadMedia onInterruptEvent;

        /// <summary>
        /// Called after each frame of video is drawn , if there is no video stream, this event will not be called 
        /// @Tip:
        /// The alignment of the frame here is 1-byte alignment
        /// </summary>
        public RenderFrame onRenderVideoFrameEvent;

        /// <summary>
        /// Called after each frame of audio is drawn , if there is no audio stream, this event will not be called 
        /// The callback thread is not the main thread
        /// </summary>
        public RenderFrame onRenderAudioFrameEvent;

        /// <summary>
        /// File type to open
        /// </summary>
        public MediaType mediaType = MediaType.LocalFile;

        /// <summary>
        /// Video rendering through this object
        /// </summary>
        public SCVideoRenderer VideoRenderer { get; private set; }

        /// <summary>
        /// Player core class, the player uses this class for media file playback, 
        /// and users can also transplant this class to WPF programs or WinForm programs.
        /// </summary>
        private SCPlayerPro core;
        private bool isFirst = true;
        private bool containVideo = true;
        private uint oidx = 0;
        private AutoResetEvent renderEvent = new AutoResetEvent(false);
        private Mutex renderMux = new Mutex();
        private bool renderClosed = true;
        private PixelFormat framePixelFormat = PixelFormat.AUTO;
        public int decoderFps;
        private int _decoderFps;
        private long lastTs;
        private SCPlayerProContext playerContext;
        /// <summary>
        /// Initialize the plugin and set up events
        /// </summary>
        private void Awake()
        {
            SCMGR.InitSCPlugins(this);
            playerContext = SCPlayerProManager.CreatePlayer();
            core = playerContext.player;
            VideoRenderer = playerContext.renderer;
#if UNITY_EDITOR
            core.EnableVsync = false;
#else
            core.EnableVsync = QualitySettings.vSyncCount == 1;
#endif
            core.onStreamFinishedEvent += OnStreamFinished;
            core.onCaptureOpenCallbackEvent += OnCaptureOpenCallback;
            core.onInterruptCallbackEvent += OnInterruptCallback;
            core.onDrawAudioFrameEvent += OnDrawAudioFrame;
            core.onDrawVideoFrameEvent += OnDrawVideoFrame;
            _loop = loop;
            lastTs = ISCNative.GetTimestamp();
        }

        public PixelFormat GetRenderPixelFormat()
        {
            return framePixelFormat;
        }

        private void OnDrawAudioFrame(IntPtr pcm, int length)
        {
            uint idx = oidx;
            core.InvokeAsync(() =>
            {
                if (idx != oidx)
                    return;
                if (isFirst && !containVideo)
                {
                    isFirst = false;
                    if (onFirstFrameRenderEvent != null)
                    {
                        try
                        {
                            onFirstFrameRenderEvent.Invoke(null);
                        }
                        catch { }
                    }
                }
                if (onRenderAudioFrameEvent != null)
                {
                    try
                    {
                        onRenderAudioFrameEvent.Invoke(null);
                    }
                    catch { }
                }
            });
        }

        private void Start()
        {
            StartCoroutine(Draw());
            if (autoOpen)
                Open(openMode);
        }

        /// <summary>
        /// When the Open function is called, 
        /// the function will be called back regardless of whether it is opened or not, 
        /// unless you call the Close function before the successful opening
        /// </summary>
        /// <param name="result">open result</param>
        /// <param name="error">error infomation</param>
        /// <param name="context">video or audio param</param>
        private void OnCaptureOpenCallback(CaptureOpenResult result, string error, OpenCallbackContext context)
        {
            if (result == CaptureOpenResult.SUCCESS)
                Debug.Log(string.Format("{0} open successed", url));
            else
                Debug.LogWarning(string.Format("{0} open failed : {1}", url, error));
            if (context != null)
            {
                containVideo = context.videoParams == IntPtr.Zero ? false : true;
            }
            if (onCaptureOpenCallbackEvent != null)
            {
                try
                {
                    onCaptureOpenCallbackEvent.Invoke(result, error, context);
                }
                catch { }
            }

        }

        /// <summary>
        /// This function will be called when the reading of the data packet fails, 
        /// such as camera unplugging, network interruption, etc.
        /// </summary>
        /// <param name="error">error log</param>
        private void OnInterruptCallback(string error)
        {
            if (onInterruptEvent != null)
            {
                try
                {
                    onInterruptEvent.Invoke(error);
                }
                catch { }
            }
        }

        /// <summary>
        /// open media
        /// </summary>
        /// <param name="url"></param>
        public void Open(MediaType openMode, string url = null)
        {
            if (onOpenEvent != null)
            {
                try
                {
                    onOpenEvent.Invoke();
                }
                catch { }
            }

            Close();
            if (core == null) return;
            isFirst = true;
            if (string.IsNullOrEmpty(url))
                url = this.url;
            core.DisableVideo = disableVideo;
            core.DisableAudio = disableAudio;
            core.DisableSubtitle = true;
            core.DefaultVideoTrack = defaultVideoTrack;
            core.DefaultAudioTrack = defaultAudioTrack;
            core.DefaultSubtitleTrack = 0;

            core.EnableHWAccel = enableHWAccel;
            core.HWAccelType = HWAccelType;
            core.OutputPixelFormat = outputPixelFormat;

            core.OpenMode = this.openMode = openMode;

            core.CameraWidth = cameraWidth;
            core.CameraHeight = cameraHeight;
            core.CameraFPS = cameraFPS;
            core.Options = options;

            core.OpenAndPlay = openAndPlay;
            core.Loop = loop;
            core.Volume = volume;
            core.Speed = speed;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            core.ExtractHWFrame = true;
#elif UNITY_ANDROID
            if(core.LParam == 0)
                core.LParam = VideoRenderer.GetANativeWindow().ToInt64();
            core.ExtractHWFrame = extractHWFrame;
#endif
            renderClosed = false;
            this.url = url;
            core.Open(openMode, url);
        }

        /// <summary>
        /// replay video
        /// </summary>
        /// <param name="paused">pause or play</param>
        public void Replay(bool paused)
        {
            if (core == null) return;
            core.Replay(paused);
        }

        /// <summary>
        /// close media
        /// </summary>
        public void Close()
        {
            if (onCloseEvent != null)
            {
                try
                {
                    onCloseEvent.Invoke();
                }
                catch { }
            }
            if (core == null) return;
            isFirst = false;
            renderMux.WaitOne();
            renderClosed = true;
            renderMux.ReleaseMutex();
            renderEvent.Set();
            core.Close();
            VideoRenderer.TerminateRenderer();
        }

        /// <summary>
        /// Seek to key frame quickly according to percentage
        /// </summary>
        /// <param name="percent"></param>
        public void SeekFastPercent(double percent)
        {
            if (core == null) return;
            core.SeekFastPercent(percent);
        }

        /// <summary>
        /// Seek to key frame quickly according to ms
        /// </summary>
        /// <param name="ms"></param>
        public void SeekFastMilliSecond(int ms)
        {
            if (core == null) return;
            core.SeekFastMilliSecond(ms);
        }

        /// <summary>
        /// play
        /// </summary>
        public void Play()
        {
            if (core == null) return;
            core.Play();
        }

        /// <summary>
        /// pause
        /// </summary>
        public void Pause()
        {
            if (core == null) return;
            core.Pause();
        }


        /// <summary>
        /// draw video frame
        /// set volume
        /// </summary>
        private void Update()
        {
            if (core == null) return;
            if (core.Volume != volume)
                core.Volume = volume;
            if (core.Speed != speed)
                core.Speed = speed;
            if (loop != _loop)
            {
                _loop = loop;
                core.Loop = loop;
            }

            if (ISCNative.GetTimestamp() - lastTs >= 1000000)
            {
                lastTs += 1000000;
                decoderFps = _decoderFps;
                _decoderFps = 0;
            }
        }

        private IEnumerator Draw()
        {
            while (true)
            {
                DrawImp();
                yield return new WaitForEndOfFrame();
            }
        }

        private void DrawImp()
        {

            if (core.Closed) return;
            if (!core.AllowDraw) return;

            SCFrame frame = core.LockFrame();
            framePixelFormat = (PixelFormat)frame.format;
            if (VideoRenderer.Renderer(frame))
            {
                if (onRendererChangedEvent != null)
                {
                    try
                    {
                        onRendererChangedEvent.Invoke(VideoRenderer.SCRenderer);
                    }
                    catch { }
                }

            }

            if (isFirst)
            {
                isFirst = false;
                if (onFirstFrameRenderEvent != null)
                {
                    try
                    {
                        onFirstFrameRenderEvent.Invoke(VideoRenderer.SCRenderer);
                    }
                    catch { }
                }
            }

            if (onRenderVideoFrameEvent != null)
            {
                try
                {
                    onRenderVideoFrameEvent.Invoke(VideoRenderer.SCRenderer);
                }
                catch { }
            }
            core.UnlockFrame();

            renderEvent.Set();

        }

        private void OnDrawVideoFrame()
        {
            _decoderFps++;
            if (!core.GetVsync()) return;
            renderMux.WaitOne();
            if (renderClosed)
            {
                renderMux.ReleaseMutex();
                return;
            }
            renderMux.ReleaseMutex();
            renderEvent.WaitOne();
        }

        /// <summary>
        /// Whether the current playback mode is looping, it will be called after media playback is complete.
        /// </summary>
        private void OnStreamFinished()
        {
            if (onStreamFinishedEvent != null)
            {
                try
                {
                    onStreamFinishedEvent.Invoke();
                }
                catch { }
            }

        }

        /// <summary>
        /// Release all resources of the player. 
        /// The user does not need to call this function. 
        /// All operations will be invalid after the function is called.
        /// </summary>
        public void ReleaseCore()
        {
            Close();
            if (core == null) return;
            SCPlayerProManager.ReleasePlayer(playerContext);
            SCMGR.RemovePlayer(this);
            core.onStreamFinishedEvent -= OnStreamFinished;
            core.onCaptureOpenCallbackEvent -= OnCaptureOpenCallback;
            core.onInterruptCallbackEvent -= OnInterruptCallback;
            core.onDrawAudioFrameEvent -= OnDrawAudioFrame;
            core.onDrawVideoFrameEvent -= OnDrawVideoFrame;
            core = null;
        }
        private void OnDestroy()
        {
            ReleaseCore();
        }
    }

}
