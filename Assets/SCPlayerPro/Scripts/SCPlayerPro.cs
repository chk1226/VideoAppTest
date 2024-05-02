using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sttplay.MediaPlayer
{
    ///// <summary>
    ///// Audio and video synchronization strategy
    ///// </summary>
    //public enum SYNCType
    //{
    //    /// <summary>
    //    /// Audio is primary, video is synchronized to audio
    //    /// </summary>
    //    AudioMaster,
    //};

    /// <summary>
    /// Type of media to open
    /// </summary>
    public enum MediaType
    {
        /// <summary>
        /// local file
        /// </summary>
        LocalFile = 0,

        /// <summary>
        /// http, https, rtp, rtsp, rtmp, hls ...
        /// </summary>
        Link,

        /// <summary>
        /// camera, virtual camera
        /// </summary>
        Camera
    }


    /// <summary>
    /// SCPlayer is a player class
    /// It is also feasible if you need to use it in WPF or WinForm,
    /// but you need to encapsulate a management class similar to UnitySCPlayerPro by yourself.
    /// We provide a relatively open development environment, 
    /// not only limited to Unity, if you want to use SCPlayerPro on other platforms or other languages, 
    /// there is no problem, just call the corresponding interface program.
    /// The interface part can refer to SCInterface.cs
    /// </summary>
    public class SCPlayerPro : ISCPlayerPro
    {

        private const int AV_SYNC_THRESHOLD_MIN = 40;
        private const int AV_SYNC_THRESHOLD_MAX = 100;
        private const int AV_SYNC_FRAMEDUP_THRESHOLD = 100;

        private struct DispatcherContext
        {
            public Action action;
            public string name;
        }

        private List<DispatcherContext> funcList = new List<DispatcherContext>();

        /// <summary>
        /// Whether to disable video 
        /// </summary>
        public bool DisableVideo { get; set; }

        /// <summary>
        /// Whether to disable audio
        /// </summary>
        public bool DisableAudio { get; set; }

        /// <summary>
        /// Whether to disable subtitle
        /// </summary>
        public bool DisableSubtitle { get; set; }

        /// <summary>
        /// The video track selected by default after opening the media
        /// </summary>
        public int DefaultVideoTrack { get; set; }

        /// <summary>
        /// The audio track selected by default after opening the media
        /// </summary>
        public int DefaultAudioTrack { get; set; }

        /// <summary>
        /// The subtitle track selected by default after opening the media
        /// </summary>
        public int DefaultSubtitleTrack { get; set; }

        /// <summary>
        /// Whether to enable hardware acceleration
        /// Not all videos support hardware acceleration.
        /// If you enable this option, hardware acceleration will be tried first, 
        /// and if it fails, the CPU will be used for decoding. 
        /// </summary>
        public bool EnableHWAccel { get; set; }

        /// <summary>
        /// The data frame decoded by hardware is actually in GPU memory, 
        /// and this mark indicates whether it is extracted into CPU memory
        /// </summary>
        public bool ExtractHWFrame { get; set; }

        /// <summary>
        /// Hardware device type when video hardware accelerates decoding 
        /// Not all of the current platforms are supported, 
        /// if the current option does not support, set as the default 
        /// </summary>
        public HWDeviceType HWAccelType { get; set; }

        /// <summary>
        /// Pixel format of output SCFrame 
        /// </summary>
        public PixelFormat OutputPixelFormat { get; set; }

        /// <summary>
        /// Media type
        /// Refer to MediaType for details
        /// </summary>
        public MediaType OpenMode { get; set; }

        /// <summary>
        /// Width camera resolution 
        /// </summary>
        public int CameraWidth { get; set; }

        /// <summary>
        /// Height camera resolution
        /// </summary>
        public int CameraHeight { get; set; }

        /// <summary>
        /// Camera fps
        /// </summary>
        public float CameraFPS { get; set; }

        /// <summary>
        /// options
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// L param
        /// </summary>
        public long LParam { get; set; }

        /// <summary>
        /// Mark whether the media is a file
        /// </summary>
        private bool IsFile { get; set; }

        /// <summary>
        /// Play directly after opening or stay at the first frame
        /// </summary>
        public bool OpenAndPlay { get; set; }

        /// <summary>
        /// Whether the media is played in a loop 
        /// This option is valid only when the mediaType is LocalOrNetworkFile 
        /// </summary>
        public bool Loop { get { return _loop; } set { _loop = value; SetLoop(_loop); } }
        private bool _loop;

        /// <summary>
        /// Current playback timestamp, valid when the mediaType is LocalOrNetFile
        /// </summary>
        public long CurrentTime { get; private set; }

        /// <summary>
        /// The total duration of the media, valid when the mediaType is LocalOrNetFile 
        /// </summary>
        public long Duration { get; private set; }

        /// <summary>
        /// Whether the marker is in a paused state 
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Mark whether player is closed
        /// Open failure is also considered not close
        /// </summary>
        public bool Closed { get; set; }

        /// <summary>
        /// Mark whether player successfully opened media
        /// </summary>
        public bool OpenSuccessed { get; set; }

        ///// <summary>
        ///// Audio and video synchronization strategy, currently only supports AudioMaster 
        ///// </summary>
        //private SYNCType syncType = SYNCType.AudioMaster;

        /// <summary>
        /// Mark whether the video can be drawn currently for external use. 
        /// When the external drawing is completed, the mark value should be set to false
        /// </summary>
        public bool AllowDraw { get; private set; }

        /// <summary>
        /// Audio volume
        /// </summary>
        public float Volume { get; set; }

        private float OVolume { get; set; }

        /// <summary>
        /// Allow vsync
        /// </summary>
        public bool EnableVsync { get; set; }

        /// <summary>
        /// Playback Speed
        /// </summary>
        public float Speed { get { return externSpeed; } set { externSpeed = value; SetVsync(); SetPlaybackSpeed(value); } }

        #region Internal use
        private IntPtr _capture;                        //Stream capture pointer
        private System.IntPtr audioPlayer;              //Audio player pointer
        private System.IntPtr resampler;                //Resampler pointer
        private System.IntPtr pcm;                      //PCM data buffer
        private System.IntPtr audioSem;                 //Audio Sem

        private bool _openNoPlay;                       //Whether to pause at the first frame
        private SCThreadHandle videoThreadHandle;       //Video thread handle
        private SCThreadHandle audioThreadHandle;       //audio thread handle
        private double _frameTime = 0;                  //Time line
        private bool waitAudioPlayer = false;           //Mark whether to wait for AudioPlayer 
        private bool isFirst = false;                   //Whether the mark is the first rendering after opening 
        private OpenCallbackContext streamCtx;          // Media information preview  
        private bool videoIsFinished = false;           //Mark the end of the video 
        private bool audioIsFinished = false;           //Mark the end of the audio 
        private bool isStep = false;                    //Mark step next video frame

        private SCClock videoClock;                     //clock
        private SCClock audioClock;                     //clock
        private int audioHWBufSize = 0;                 //audio device hardware buffer size
        private int bytesPerSec = 0;                    //The amount of data required for 1s audio
        private int sampleRate = 0;                     //The audio sample rate
        private double audioDiffThreshold;              //This attribute has no effect now 
        private bool nextClearPCMCache = false;         //Mark the next audio callback to clean up the data
        private bool isSetPaused = false;
        private bool isSetPausedState;

        private readonly Mutex mux = new Mutex();       //Lock video frame
        private IntPtr frameBK;                         //Backup video frame
        private double audioClockTs = 0;
        private static Action<string> cameraInfoCallback;

        private float internalSpeed = 1.0f, externSpeed = 1.0f;
        private bool vsync = false;
        private bool canSpeed;
        private bool audioEOF = false;
        private AudioOutputFormat audioOutputFmt = AudioOutputFormat.FLT;
        private int packetCacheCount = 1;
        private int fixedWidth = 0;
        private int fixedHeight = 0;
        private static ISCNative.SCOption[] internalOptions = {
            new ISCNative.SCOption("packet_cache",               ISCNative.HAS_ARG,                ""), 
            new ISCNative.SCOption("",                           ISCNative.FINISH_FLG,             "") };
        #endregion // Internal use


        /// <summary>
        /// Called when a video frame needs to be rendered, of course, you can choose to do nothing.
        /// @Tip:
        /// The buffer here is the video data. 
        /// The linesize[0] in the frame may be different from the width. 
        /// The alignment is related to the CPU. 
        /// You can use these video frame data directly. 
        /// If you expect to use 1-byte aligned video frames then please refer to onRenderFrameEvent in UnitySCPlayerPro
        /// </summary>
        public event Action onDrawVideoFrameEvent;

        /// <summary>
        /// Called when a audio frame needs to be rendered, of course, you can choose to do nothing.
        /// @Tip:
        /// The buffer here is audio data, the format is s16, 
        /// if you need to use these data, you can use Marshal.
        /// Copy to copy the data, so that you can make full use of the data
        /// </summary>
        public event Action<IntPtr, int> onDrawAudioFrameEvent;

        /// <summary>
        /// Called when the video has finished playing, whether looping or not 
        /// </summary>
        public event Action onStreamFinishedEvent;

        /// <summary>
        /// Called when player demux succeeds or failed
        /// </summary>
        public event Action<CaptureOpenResult, string, OpenCallbackContext> onCaptureOpenCallbackEvent;

        /// <summary>
        /// Called when audio player succeeds or failed
        /// </summary>
        public event Action<AudioPlayerOpenResult, string, PlayerParams> onAudioPlayerOpenCallbackEvent;

        /// <summary>
        /// Called when player demux read pakcet failed
        /// </summary>
        public event Action<string> onInterruptCallbackEvent;

        /// <summary>
        /// Initialize player
        /// We have to set some parameters and set some handles
        /// Every step here is important
        /// </summary>
        public SCPlayerPro()
        {
            SCMGR.AddPlayer(this);
            DisableAudio = false;
            DisableVideo = false;
            DisableSubtitle = true;
            DefaultVideoTrack = DefaultAudioTrack = DefaultSubtitleTrack = 0;

            CameraWidth = 640;
            CameraHeight = 480;
            CameraFPS = 30;

            EnableHWAccel = true;
            ExtractHWFrame = true;

            HWAccelType = HWDeviceType.AUTO;
            OutputPixelFormat = PixelFormat.AUTO;
            OpenMode = MediaType.LocalFile;

            OpenAndPlay = true;
            AllowDraw = false;
            OVolume = Volume = 1.0f;

            Speed = 1.0f;
            CurrentTime = Duration = 0;
            Closed = true;
            OpenSuccessed = false;
            EnableVsync = true;
            _capture = ISCNative.CreateStreamCapture();
            audioPlayer = ISCNative.CreateAudioPlayer();
            resampler = ISCNative.CreateResampler();
            pcm = ISCNative.CreateByteArray();
            frameBK = ISCNative.CreateSCFrame(IntPtr.Zero, 0, (int)SCFrameFlag.Move);
        }

        /// <summary>
        /// Lock current video frame, Use this method must be used together with UnlockFrame
        /// The update of video frame drawing is not necessarily in the main thread, so thread safety must be ensured
        /// </summary>
        /// <returns>current video frame</returns>
        public SCFrame LockFrame()
        {
            mux.WaitOne();
            AllowDraw = false;
            return Marshal.PtrToStructure<SCFrame>(frameBK);
        }

        /// <summary>
        /// Unlock current video frame, Use this method must be used together with LockFrame
        /// The update of video frame drawing is not necessarily in the main thread, so thread safety must be ensured
        /// </summary>
        /// <returns>current video frame</returns>
        public void UnlockFrame()
        {
            mux.ReleaseMutex();
        }

        private void SetPlaybackSpeed(float speed)
        {
            internalSpeed = speed;
            if (internalSpeed > 2.0f)
                internalSpeed = 2.0f;
            if (internalSpeed < 0.5f)
                internalSpeed = 0.5f;
        }

        private void SetVsync()
        {
            bool lastVsync = vsync;
            vsync = false;
            if (streamCtx != null && streamCtx.videoParams != IntPtr.Zero)
            {
                VideoParams vp = Marshal.PtrToStructure<VideoParams>(streamCtx.videoParams);
                if (Math.Abs(vp.fps * externSpeed - SCMGR.GetMaxDisplayFrequency()) <= 1)
                {
                    if(EnableVsync)
                        vsync = true;
                }
            }
            if (lastVsync != vsync)
                _frameTime = 0;
        }

        public bool GetVsync()
        {
            return vsync;
        }
        public IntPtr GetAVFramePointer()
        {
            return frameBK;
        }

        /// <summary>
        /// Open media
        /// </summary>
        /// <param name="url">When Null is passed, it will be opened by default according to the global variable url</param>
        public void Open(MediaType openMode, string url)
        {
            this.OpenMode = openMode;
            if (_capture == IntPtr.Zero) return;
            if(string.IsNullOrEmpty(url))
                url = "";
            Close();
            isSetPaused = false;
            Closed = false;
            SCConfiguration config = new SCConfiguration();
            config.disableAudio = DisableAudio ? 1 : 0;
            config.disableVideo = DisableVideo ? 1 : 0;
            config.disableSubtitle = DisableSubtitle ? 1 : 0;
            config.videoTrack = DefaultVideoTrack;
            config.audioTrack = DefaultAudioTrack;
            config.subtitleTrack = DefaultSubtitleTrack;
            config.enableHWAccel = EnableHWAccel ? 1 : 0;
            config.extractHWFrame = ExtractHWFrame ? 1 : 0;
            config.hwaccelType = (int)HWAccelType;
            config.outputPixfmt = (int)OutputPixelFormat;
            config.openMode = (int)openMode;
            config.lparam = LParam;
            packetCacheCount = 30;
            if(openMode == MediaType.LocalFile)
            {
                ISCNative.SetOpenOptions(_capture, IntPtr.Zero);
            }
            else if(openMode == MediaType.Link)
            {
                List<ISCNative.SCOption> opts = new List<ISCNative.SCOption>();
                ISCNative.GetOptions(Options, internalOptions, opts);
                foreach (var item in opts)
                {
                    if (item.name == "packet_cache")
                    {
                        int.TryParse(item.value, out packetCacheCount);
                        if (packetCacheCount <= 0) packetCacheCount = 0;
                    }
                }
                IntPtr options = ISCNative.StringToIntPtr(Options);
                ISCNative.SetOpenOptions(_capture, options);
                ISCNative.ReleaseStringIntPtr(options);
            }
            else if(openMode == MediaType.Camera)
            {
                config.cameraWidth = CameraWidth;
                config.cameraHeight = CameraHeight;
                config.cameraFPS = CameraFPS;
                IntPtr options = ISCNative.StringToIntPtr(Options);
                ISCNative.SetOpenOptions(_capture, options);
                ISCNative.ReleaseStringIntPtr(options);
            }

            _openNoPlay = !OpenAndPlay;
            isFirst = true;
            audioClock = new SCClock();
            videoClock = new SCClock();
            SetLoop(Loop);
            AllowDraw = false;
            OVolume = Volume;
            var pconfig = ISCNative.StructureToIntPtr(config);
            var purl = ISCNative.StringToIntPtr(url.Trim());
            ISCNative.AsyncOpenStreamCapture(_capture, purl, pconfig);
            ISCNative.ReleaseStructIntPtr(pconfig);
            ISCNative.ReleaseStringIntPtr(purl);
        }

        /// <summary>
        /// Close media
        /// </summary>
        public void Close()
        {
            isFirst = false;
            Closed = true;
            OpenSuccessed = false;
            isSetPaused = false;
            CurrentTime = 0;
            fixedHeight = fixedWidth = 0;
            SCMGR.ReleaseThreadHandle(audioThreadHandle);
            SCMGR.ReleaseThreadHandle(videoThreadHandle);
            videoThreadHandle = audioThreadHandle = null;
            WakeAll(true);
            streamCtx = null;
            if (_capture == IntPtr.Zero) return;

            ISCNative.PausedAudioPlayer(audioPlayer, 1);
            ISCNative.CloseStreamCapture(_capture);
            ISCNative.CloseAudioPlayer(audioPlayer);
            ISCNative.CloseResampler(resampler);
            nextClearPCMCache = true;
            audioClock = videoClock = null;
            audioSem = IntPtr.Zero;
        }

        /// <summary>
        /// driver
        /// </summary>
        public void Update()
        {
            if (Closed) return;
            IntPtr ec = IntPtr.Zero;
            if(ISCNative.PeekCoreEvent(_capture, ref ec) >= 0)
            {
                EventContext ecctx = Marshal.PtrToStructure<EventContext>(ec);
                if (ecctx.type == (int)EventCoreType.Open)
                {
                    OpenCallbackContext cbctx = null;
                    CaptureOpenResult state = (CaptureOpenResult)ecctx.p1.ToInt64();
                    string error = Marshal.PtrToStringAnsi(ecctx.p2);
                    if (state == CaptureOpenResult.SUCCESS)
                        cbctx = Marshal.PtrToStructure<OpenCallbackContext>(ecctx.p3);
                    CaptureOpenCallback(state, error, cbctx);
                }
                else if(ecctx.type == (int)EventCoreType.Interrupt)
                {
                    string error = Marshal.PtrToStringAnsi(ecctx.p1);
                    InterruptCallback(error);
                }
            }

            if(ISCNative.PeekAudioEvent(audioPlayer, ref ec) >= 0)
            {
                EventContext ecctx = Marshal.PtrToStructure<EventContext>(ec);
                if (ecctx.type == (int)EventAudioType.Open)
                {
                    AudioPlayerOpenResult code = (AudioPlayerOpenResult)ecctx.p1.ToInt64();
                    PlayerParams pp = null;
                    string error = Marshal.PtrToStringAnsi(ecctx.p2);
                    if (code == AudioPlayerOpenResult.SUCCESS)
                        pp = Marshal.PtrToStructure<PlayerParams>(ecctx.p3);
                    AudioPlayerOpenCallback(code, error, pp);
                }
            }

            WakeAll(false);
        }

        /// <summary>
        /// Call this function for cross-threading
        /// </summary>
        /// <param name="func">Action</param>
        public void InvokeAsync(Action func, string name = "Unknow")
        {
            lock (funcList)
            {
                DispatcherContext context = new DispatcherContext();
                context.action = func;
                context.name = name;
                funcList.Add(context);
            }
        }

        /// <summary>
        /// Call this function through the lifecycle function
        /// </summary>
        private void WakeAll(bool clear)
        {
            List<DispatcherContext> temp = new List<DispatcherContext>();
            lock (funcList)
            {
                temp.AddRange(funcList);
                funcList.Clear();
            }
            while (temp.Count > 0)
            {
                DispatcherContext context = temp[0];
                if (context.action == null)
                {
                    ISCNative.SCLog(LogLevel.Warning, context.name + " is null");
                }
                else
                {
                    if(!clear)
                        context.action();
                }
                temp.RemoveAt(0);
            }
        }

        /// <summary>
        /// Set whether the video is played in a loop
        /// </summary>
        /// <param name="isLoop">loop or not</param>
        private void SetLoop(bool isLoop)
        {
            if (_capture == System.IntPtr.Zero)
                return;
            _loop = isLoop;
            ISCNative.SetCaptureLoop(_capture, isLoop ? 1 : 0);
        }

        /// <summary>
        /// stream is finished
        /// </summary>
        /// <param name="type"></param>
        private void OnStreamFinished(FrameType type)
        {
            if (streamCtx.videoParams == IntPtr.Zero) videoIsFinished = true;
            if (streamCtx.audioParams == IntPtr.Zero) audioIsFinished = true;
            if (type == FrameType.Video) videoIsFinished = true;
            if (type == FrameType.Audio) audioIsFinished = true;
            if (audioIsFinished && videoIsFinished)
            {
                InvokeAsync(() =>
                {
                    if (onStreamFinishedEvent != null)
                    {
                        try
                        {
                            onStreamFinishedEvent();
                        }
                        catch { }
                    }
                    if (!Loop)
                    {
                        CurrentTime = Duration;
                        audioEOF = true;
                    }
                }, "OnStreamFinished");
                videoIsFinished = audioIsFinished = false;
            }
        }

        /// <summary>
        /// update pts 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pts"></param>
        private void UpdatePTS(FrameType type, long pts)
        {
            if (streamCtx.videoParams != IntPtr.Zero)
            {
                if (type == FrameType.Video)
                    CurrentTime = pts;
            }
            else if (streamCtx.audioParams != IntPtr.Zero)
            {
                if (type == FrameType.Audio)
                    CurrentTime = pts;
            }
            if (CurrentTime > Duration)
                CurrentTime = Duration;
        }

        /// <summary>
        /// Video thread 
        /// </summary>
        private void VideoThread()
        {
            int remainingTime = 0;
            _frameTime = 0;
            int lastMaxFramerate = SCMGR.GetMaxDisplayFrequency();
            long ts = ISCNative.GetTimestamp();
            while (!Closed)
            {
                if (waitAudioPlayer)
                {
                    Thread.Sleep(2);
                    continue;
                }
                if(ISCNative.GetTimestamp() - ts > 1000000)
                {
                    ts += 1000000;
                    int fr = SCMGR.GetMaxDisplayFrequency();
                    if (fr != lastMaxFramerate)
                        SetVsync();
                    lastMaxFramerate = fr;
                }
                if (remainingTime > 0)
                {
                    if (vsync) remainingTime = Math.Max(remainingTime - 3, 2);
                    Thread.Sleep(remainingTime);
                }
                remainingTime = 10;
                if (streamCtx.realtime && packetCacheCount >= 0)
                {
                    int pCount = 0, fCount = 0;
                    ISCNative.GetPFCount(_capture, (int)FrameType.Video, ref pCount, ref fCount);
                    if (pCount > packetCacheCount)
                        Speed = 1.05f;
                    else if (pCount < packetCacheCount)
                        Speed = 0.95f;
                    else
                        Speed = 1;
                }
                if (!IsPaused)
                    RenderVideo(ref remainingTime);
            }
            LockFrame();
            UnlockFrame();
            ISCNative.ImageCopy(frameBK, IntPtr.Zero);
        }

        /// <summary>
        /// Audio thread
        /// </summary>
        private void AudioThread()
        {
            IntPtr buffer = IntPtr.Zero;
            int length = 0;
            while (!Closed)
            {
                if (waitAudioPlayer)
                {
                    Thread.Sleep(2);
                    continue;
                }
                if (ISCNative.WaitInternalSemTimeout(audioSem, 2, ref buffer, ref length) != 0)
                    continue;
                AudioPlayCallback(buffer, length);
                ISCNative.PostExternalSem(audioSem);
            }
        }

        /// <summary>
        /// Control video playback 
        /// </summary>
        /// <param name="remaining_time">remaining time</param>
        private void RenderVideo(ref int remaining_time, bool dropOnly = false)
        {
            SCFrame frame = new SCFrame();
            SCFrame lastFrame = new SCFrame();
            int ret = TryGrabFrame(FrameType.Video, ref frame);
            if (ret == 0)
                return;
            if (ret < 0)
                throw new Exception("TryGrabFrame Error");


            ret = TryGrabLastFrame(FrameType.Video, ref lastFrame);
            if (ret < 0)
                throw new Exception("TryGrabFrame Error");

            if (dropOnly)
                goto finished;
            if (frame.context_type == (int)FrameContextType.EOF)
            {
                OnStreamFinished(FrameType.Video);
                ISCNative.FrameMoveToLast(_capture, (int)FrameType.Video);
                return;
            }
            UpdatePTS((FrameType)frame.media_type, frame.pts_ms);
            double delay = LastDuration(lastFrame, frame);
            delay /= internalSpeed;
            double time = ISCNative.GetTimestampUTC() / 1000;
            if (time < _frameTime + delay)
            {
                remaining_time = Math.Min((int)(_frameTime + delay - time), remaining_time);
                return;
            }

            float adjust = canSpeed ? externSpeed : 1.0f;
            if (_frameTime <= 0)
                _frameTime = time;
            if (!vsync)
                _frameTime += delay;


            videoClock.SetClock(frame.pts_ms);

            mux.WaitOne();
            var ptr = ISCNative.StructureToIntPtr(frame);
            ISCNative.ImageCopy(frameBK, ptr);
            ISCNative.ReleaseStructIntPtr(ptr);
            AllowDraw = true;
            mux.ReleaseMutex();
            if (onDrawVideoFrameEvent != null)
            {
                try
                {
                    onDrawVideoFrameEvent();
                }
                catch { }
            }
            if (isFirst)
            {
                isFirst = false;
                InvokeAsync(() =>
                {
                    if (!Closed && audioPlayer != IntPtr.Zero)
                        ISCNative.PausedAudioPlayer(audioPlayer, 0);
                }, "RenderVideo");
            }


        finished:
            if (!vsync)
                remaining_time = 0;
            ISCNative.FrameMoveToLast(_capture, (int)FrameType.Video);
            if (isStep)
            {
                isStep = false;
                IsPaused = true;
            }
        }

        /// <summary>
        /// Calculate the time interval between two frames
        /// </summary>
        /// <param name="lastFrame">last frame</param>
        /// <param name="frame">current frame</param>
        /// <returns></returns>
        private double LastDuration(SCFrame lastFrame, SCFrame frame)
        {
            double duration = frame.pts_ms - lastFrame.pts_ms;
            if (double.IsNaN(duration) || duration <= 0 || duration > 10000 || duration > lastFrame.duration * 2)
                return lastFrame.duration;
            else
                return duration;
        }

        ///// <summary>
        ///// Calculate the delay time 
        ///// </summary>
        ///// <param name="delay"></param>
        ///// <returns></returns>
        //private double ComputeTargetDelay(double delay)
        //{
        //    //Time difference between video clock and main clock 
        //    double diff = 0;
        //    if (streamCtx.realtime && Nobuffer)
        //        diff = videoClock.GetClock() - videoClock.GetClock();
        //    else
        //        diff = videoClock.GetClock() - GetMasterClock().GetClock();
        //    double sync_threshold = Math.Max(AV_SYNC_THRESHOLD_MIN, Math.Min(AV_SYNC_THRESHOLD_MAX, (float)delay));

        //    //When the video is slower than the audio and exceeds the threshold 
        //    if (diff <= -sync_threshold)
        //    {
        //        delay = Math.Max(0, (float)(delay + diff));
        //        //Debug.LogWarningFormat("Video is too slow, more than the threshold:{0:F1}", -diff / 1000);
        //    }
        //    else if (diff >= sync_threshold && delay > AV_SYNC_FRAMEDUP_THRESHOLD)
        //        delay = delay + diff;
        //    //When video is faster than audio 
        //    else if (diff >= sync_threshold)
        //        delay = 1.5 * delay;

        //    return delay;
        //}
        //
        ///// <summary>
        ///// Get master clock
        ///// </summary>
        ///// <returns></returns>
        //private SCClock GetMasterClock()
        //{
        //    SCClock clock = null;
        //    switch (syncType)
        //    {
        //        case SYNCType.AudioMaster:
        //            clock = audioClock;
        //            break;
        //    }
        //    return clock;
        //}


        /// <summary>
        /// try grab frame
        /// </summary>
        /// <param name="type">frame type</param>
        /// <param name="frame">frame</param>
        /// <returns>
        /// return 0 capture is null, return -1 error, 0 is ok
        /// </returns>
        private int TryGrabFrame(FrameType type, ref SCFrame frame)
        {
            if (_capture == IntPtr.Zero)
                return 0;
            IntPtr ptr = new IntPtr();
            int ret = ISCNative.TryGrabFrame(_capture, (int)type, ref ptr);
            if (ret > 0)
            {
                frame = Marshal.PtrToStructure<SCFrame>(ptr);
            }
            return ret;
        }

        /// <summary>
        /// try grab last frame
        /// </summary>
        /// <param name="type">frame type</param>
        /// <param name="frame">frame</param>
        /// <returns>
        /// return 0 capture is null, return -1 error, 0 is ok
        /// </returns>
        private int TryGrabLastFrame(FrameType type, ref SCFrame frame)
        {
            if (_capture == IntPtr.Zero)
                return 0;
            IntPtr ptr = new IntPtr();
            int ret = ISCNative.TryGrabLastFrame(_capture, (int)type, ref ptr);
            if (ret > 0)
                frame = Marshal.PtrToStructure<SCFrame>(ptr);
            return ret;
        }

        public void FrameMoveToLast(FrameType type)
        {
            if (_capture == IntPtr.Zero)
                return;
            ISCNative.FrameMoveToLast(_capture, (int)type);
        }

        /// <summary>
        /// This function is valid if and only when mediaType is LocalOrNetworkFile
        /// seek media to first frame
        /// </summary>
        /// <param name="paused">paused or not</param>
        public void Replay(bool paused)
        {

            bool isFirstFrame = CurrentTime == 0;
            if (!isFirstFrame)
                SeekFastPercent(0);

            IsPaused = paused;
            if (IsPaused)
            {
                if (!isFirstFrame)
                    StepNextFrame();
            }
            else
                Play();
        }

        /// <summary>
        /// Seek to key frame quickly according to percentage
        /// </summary>
        /// <param name="percent"></param>
        public void SeekFastPercent(double percent)
        {
            if (_capture == System.IntPtr.Zero || streamCtx == null)
                return;
            ISCNative.SeekFastPercent(_capture, percent);
            CurrentTime = (long)(Duration * percent);
            SeekReset();
        }

        /// <summary>
        /// Seek to key frame quickly according to ms
        /// </summary>
        /// <param name="ms"></param>
        public void SeekFastMilliSecond(int ms)
        {
            if (_capture == System.IntPtr.Zero || streamCtx == null)
                return;
            if (ms < 0) ms = 0;
            if (ms > Duration) ms = (int)Duration;
            ISCNative.SeekFastMs(_capture, ms);
            CurrentTime = ms;
            SeekReset();
        }

        /// <summary>
        /// Play media
        /// </summary>
        public void Play()
        {
            if (_openNoPlay)
                _openNoPlay = false;
            IsPaused = false;
            isStep = false;
            isSetPaused = true;
            isSetPausedState = false;
            _frameTime = 0;
        }

        /// <summary>
        /// Pause media
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            isSetPaused = true;
            isSetPausedState = true;
        }

        /// <summary>
        /// Reset related attributes 
        /// </summary>
        private void SeekReset()
        {
            if (streamCtx.audioParams != IntPtr.Zero)
                nextClearPCMCache = true;
            audioIsFinished = false;
            videoIsFinished = false;
            _frameTime = 0;

            if (IsPaused && streamCtx.videoParams != IntPtr.Zero)
                StepNextFrame();
        }

        /// <summary>
        /// seek to the next video frame 
        /// </summary>
        private void StepNextFrame()
        {
            if (streamCtx.videoParams == IntPtr.Zero) return;
            IsPaused = false;
            isStep = true;
        }


        /// <summary>
        /// Release all resources of the player. 
        /// All operations will be invalid after the function is called.
        /// </summary>
        public void Release()
        {
            Close();
            if (_capture == IntPtr.Zero) return;
            ISCNative.ReleaseStreamCapture(_capture);
            _capture = IntPtr.Zero;
            ISCNative.ReleaseAudioPlayer(audioPlayer);
            audioPlayer = IntPtr.Zero;
            ISCNative.ReleaseResampler(resampler);
            resampler = IntPtr.Zero;
            ISCNative.ReleaseByteArray(pcm);
            pcm = IntPtr.Zero;
            ISCNative.ReleaseSCFrame(frameBK);
            frameBK = IntPtr.Zero;
            SCMGR.RemovePlayer(this);
        }

        /// <summary>
        /// When the Open function is called, 
        /// the function will be called back regardless of whether it is opened or not, 
        /// unless you call the Close function before the successful opening
        /// </summary>
        /// <param name="state">open result</param>
        /// <param name="error">error infomation</param>
        /// <param name="context">video or audio param</param>
        private void CaptureOpenCallback(CaptureOpenResult state, string error, OpenCallbackContext ctx)
        {
            IsFile = ctx == null ? false : ctx.localfile;
            if (state == CaptureOpenResult.SUCCESS)
            {
                OpenSuccessed = true;
                streamCtx = ctx;
                if (streamCtx.localfile)
                {
                    if (internalSpeed > 0.99f && internalSpeed < 1.01f)
                        internalSpeed = 1;
                }
                SetVsync();
                Duration = ctx.duration;
                IsPaused = isSetPaused ? isSetPausedState : _openNoPlay;
                if (IsPaused)
                    StepNextFrame();
                canSpeed = streamCtx.localfile;

                if (onCaptureOpenCallbackEvent != null)
                {
                    try
                    {
                        onCaptureOpenCallbackEvent(state, error, ctx);
                    }
                    catch { }
                }
                waitAudioPlayer = true;
                if (ctx.videoParams != IntPtr.Zero)
                {
                    videoThreadHandle = SCMGR.CreateThreadHandle(VideoThread);
                }
                if (ctx.audioParams != IntPtr.Zero)
                {
                    ISCNative.AsyncOpenAudioPlayer(audioPlayer, IntPtr.Zero, (int)audioOutputFmt, ctx.audioParams, ctx.videoParams != IntPtr.Zero ? 1 : 0);
                    audioSem = ISCNative.GetAudioPlayerSem(audioPlayer);
                    audioThreadHandle = SCMGR.CreateThreadHandle(AudioThread);
                }
                else
                    waitAudioPlayer = false;
            }
            else
            {
                if (onCaptureOpenCallbackEvent != null)
                {
                    try
                    {
                        onCaptureOpenCallbackEvent(state, error, ctx);
                    }
                    catch { }
                }
            }
        }

        public void InterruptCallback(string error)
        {
            if (Closed) return;
            if (onInterruptCallbackEvent != null)
            {
                try
                {
                    onInterruptCallbackEvent(error);
                }
                catch
                { }
            }
        }

        /// <summary>
        /// When the Open function is called, 
        /// the function will be called back regardless of whether it is opened or not, 
        /// unless you call the Close function before the successful opening
        /// </summary>
        /// <param name="code">open result</param>
        /// <param name="error">error infomation</param>
        /// <param name="param">video or audio param</param>
        public void AudioPlayerOpenCallback(AudioPlayerOpenResult code, string error, PlayerParams param)
        {
            if (code == AudioPlayerOpenResult.SUCCESS)
            {
                AudioParams dstap = Marshal.PtrToStructure<AudioParams>(param.dstap);
                audioHWBufSize = param.hwSize;
                bytesPerSec = dstap.freq * ISCNative.GetBytesPerSample((int)audioOutputFmt) * dstap.channels;
                sampleRate = dstap.freq;
                audioDiffThreshold = (double)audioHWBufSize / bytesPerSec * 1000;
                int ret = ISCNative.OpenResampler(resampler, param.srcap, param.dstap);
                if (ret < 0)
                {
                    ISCNative.SCLog(LogLevel.Critical, "OpenResampler failed");
                }
            }
            waitAudioPlayer = false;
            if (onAudioPlayerOpenCallbackEvent != null)
            {
                try
                {
                    onAudioPlayerOpenCallbackEvent(code, error, param);
                }
                catch
                { }
            }
        }

        /// <summary>
        /// AudioPlayer callback function 
        /// Fill in the data for the audioplayer in this function
        /// </summary>
        /// <param name="buffer">Target buffer pointer </param>
        /// <param name="len">buffer len</param>
        public void AudioPlayCallback(System.IntPtr buffer, int len)
        {
            ISCNative.Memset(buffer, 0, len);
            if (SCMGR.IsPaused)
                return;

            if (nextClearPCMCache)
            {
                ISCNative.ClearByteArray(pcm);
                ISCNative.ResampleClear(resampler);
                nextClearPCMCache = false;
            }

            long audioCallbackTime = ISCNative.GetTimestampUTC();

            while (!Closed && !IsPaused && ISCNative.GetByteArraySize(pcm) < len)
            {
                SCFrame frame = new SCFrame();
                int ret = TryGrabFrame(FrameType.Audio, ref frame);
                if (ret < 0)
                    break;
                if (ret == 0)
                {
                    if (audioEOF && !Loop)
                    {
                        ISCNative.Memset(buffer, 0, len);
                        return;
                    }
                    Thread.Sleep(2);
                    continue;
                }
                audioEOF = false;
                if (frame.context_type == (int)FrameContextType.EOF)
                {
                    OnStreamFinished(FrameType.Audio);
                    ISCNative.FrameMoveToLast(_capture, (int)FrameType.Audio);
                    break;
                }
                UpdatePTS((FrameType)frame.media_type, frame.pts_ms);
                float realSpeed = canSpeed ? externSpeed : 1.0f;
                if (streamCtx.videoParams != IntPtr.Zero)
                {
                    int offset = 0;
                    int[] split = new int[]{ 100 + offset, 50 + offset, 0 + offset, -50 + offset};
                    double delta = videoClock.GetClock() - audioClock.GetClock();
                    float rate = externSpeed * 0.05f;
                    if (delta > split[0])
                        realSpeed += rate * 2;
                    else if (delta > split[1])
                        realSpeed += rate;
                    else if (delta < split[3])
                        realSpeed -= rate * 2;
                    else if (delta < split[2])
                        realSpeed -= rate;
                    //printf("%llf\t%d\n", delta, vsync);
                }
                ISCNative.ResamplePush(resampler, frame.data, frame.nb_samples);
                IntPtr resampleDataPtr = ISCNative.ResampleGet(resampler, -1);
                ResampleData rd = Marshal.PtrToStructure<ResampleData>(resampleDataPtr);
                IntPtr soundDataPtr = ISCNative.ResampleTempo(resampler, rd.data[0], rd.nbSamples, realSpeed);
                if (soundDataPtr != IntPtr.Zero)
                {
                    SoundData sd = Marshal.PtrToStructure<SoundData>(soundDataPtr);
                    ISCNative.PushDataToByteArray(pcm, sd.data, sd.length);
                    if (onDrawAudioFrameEvent != null)
                    {
                        try
                        {
                            onDrawAudioFrameEvent(sd.data, sd.length);
                        }
                        catch { }
                    }
                }
                ISCNative.ResamplePop(resampler, rd.nbSamples);
                audioClockTs = frame.pts_ms + (double)frame.nb_samples / frame.sample_rate * 1000;
                ISCNative.FrameMoveToLast(_capture, (int)FrameType.Audio);
            }
            if (IsPaused)
            {
                if (onDrawAudioFrameEvent != null)
                {
                    try
                    {
                        onDrawAudioFrameEvent(IntPtr.Zero, 0);
                    }
                    catch { }
                }
                ISCNative.Memset(buffer, 0, len);
                return;
            }

            int minLen = Math.Min(len, ISCNative.GetByteArraySize(pcm));

            ISCNative.MixAudioFormat(buffer, ISCNative.GetByteArrayData(pcm), minLen, OVolume, Volume, (int)audioOutputFmt);
            OVolume = Volume;
            ISCNative.RemoveRangeFromByteArray(pcm, minLen);

            double noplayBuffSize = audioHWBufSize + ISCNative.GetByteArraySize(pcm);
            int noplayms = (int)(noplayBuffSize / bytesPerSec * 1000);
            int noplaySamples = ISCNative.GetUnprocessedSamples(resampler) * 1000 / sampleRate;
            double crtpts = audioClockTs - noplayms - noplaySamples;
            if (audioClock != null) audioClock.SetClockAt(crtpts, audioCallbackTime / 1000);
        }

        public static List<string> GetDeviceList(DeviceType type)
        {
            List<string> devices = new List<string>();
            int count = ISCNative.GetCameraCount();
            for (int i = 0; i < count; i++)
                devices.Add(Marshal.PtrToStringAnsi(ISCNative.GetCameraName(i, 0)));
            return devices;
        }

        public static void GetCameraInfomation(string deviceName, Action<string> cb)
        {
            cameraInfoCallback = cb;
            ISCNative.CamInfoCallbackDelegate icb = GetCameraInfoCallback;
            ISCNative.GetCameraInfomation(ISCNative.StringToByteArray(deviceName), IntPtr.Zero, icb, 0);
        }

        private static void GetCameraInfoCallback(IntPtr user, IntPtr info)
        {
            string infostr = Marshal.PtrToStringUni(info);
            byte[] bs = System.Text.Encoding.Unicode.GetBytes(infostr);
            infostr = System.Text.Encoding.UTF8.GetString(bs);
            if (cameraInfoCallback != null) cameraInfoCallback(infostr.Remove(infostr.LastIndexOf('\n')));
        }
    }
}
