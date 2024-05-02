using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

/// <summary>
/// This file defines the C# interface of the external function,
/// Of course you can port it to other programs
/// </summary>
namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// log level , such as Log, LogWarning, LogError
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// All hardware deviceType data types are enumerated here
    /// This does not mean that the platform supports all types
    /// </summary>
    public enum HWDeviceType
    {
        AUTO = 0,
        VDPAU,
        CUDA,
        VAAPI,
        DXVA2,
        QSV,
        VIDEOTOOLBOX,
        D3D11VA,
        DRM,
        OPENCL,
        MEDIACODEC,
        VULKAN
    }

    /// <summary>
    /// All Output pixel format data types are enumerated here
    /// Automatic selection will be set as the default output format. 
    /// If the replaced output format is not in the enumerated range, 
    /// it will be automatically selected as BGRA again. 
    /// </summary>
    public enum PixelFormat
    {
        AUTO = -1,

        YUV420P = 0,
        YUV422P = 4,
        YUV444P = 5,

        YUYV422 = 1,
        UYVY422 = 15,

        GRAY8 = 8,

        YUVJ420P = 12,
        YUVJ422P = 13,
        YUVJ444P = 14,

        NV12 = 23,
        NV21 = 24,

        RGB24 = 2,
        BGR24 = 3,

        ARGB = 25,
        RGBA = 26,
        ABGR = 27,
        BGRA = 28,


        PIX_FMT_DXVA2 = 51,
        PIX_FMT_CUDA = 117,
        PIX_FMT_MEDIACODEC = 165,
        PIX_FMT_D3D11 = 172,
    };

    public enum SampleFormat
    {

        SAM_FMT_UNKOW = -1,
        SAM_FMT_U8,          ///< unsigned 8 bits
        SAM_FMT_S16,         ///< signed 16 bits
        SAM_FMT_S32,         ///< signed 32 bits
        SAM_FMT_FLT,         ///< float
        SAM_FMT_DBL,         ///< double

        SAM_FMT_U8P,         ///< unsigned 8 bits, planar
        SAM_FMT_S16P,        ///< signed 16 bits, planar
        SAM_FMT_S32P,        ///< signed 32 bits, planar
        SAM_FMT_FLTP,        ///< float, planar
        SAM_FMT_DBLP,        ///< double, planar
        SAM_FMT_S64,         ///< signed 64 bits
        SAM_FMT_S64P,        ///< signed 64 bits, planar
    };

    public enum AudioOutputFormat
    {
        S16 = SampleFormat.SAM_FMT_S16,
        S32 = SampleFormat.SAM_FMT_S32,
        FLT = SampleFormat.SAM_FMT_FLT
    }

    /// <summary>
    /// Create SCFrame mark, mark whether to copy or move data when calling ImageCopy
    /// </summary>
    public enum SCFrameFlag
    {
        Copy = 0,
        Move
    }

    /// <summary>
    /// open configuration
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class SCConfiguration
    {
        public int disableVideo = 0;
        public int disableAudio = 0;
        public int disableSubtitle = 1;
        public int videoTrack = 0;
        public int audioTrack = 0;
        public int subtitleTrack = 0;
        public int enableHWAccel = 1;
        public int hwaccelType = (int)HWDeviceType.AUTO;
        public int extractHWFrame = 1;       //extract hwaccel frame, there set 1
        public int outputPixfmt = (int)PixelFormat.AUTO;
        public int openMode = (int)MediaType.LocalFile;
        public int cameraWidth = 640;
        public int cameraHeight = 480;
        public float cameraFPS = 30.0f;
        public long lparam = 0;
    };

    public enum CodecType
    {
        Video,
        Audio
    }

    public enum CodecID
    {
        H264 = 27,
        HEVC = 173,

        AAC = 86018
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SCCodecInfo
    {
        public int type;
        public int codecID;
        public int lowDelay;
        public int threadCount;
        public int enableHWAccel;
        public int hwaccelType;
        public int extractHWFrame;
        public int outputPixfmt;
    }

    /// <summary>
    /// Basic video parameters 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class VideoParams
    {
        public int pixfmt;
        public int width;
        public int height;
        public float fps;
    };

    /// <summary>
    /// Basic audio parameters 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class AudioParams
    {
        public int freq;
        public int channels;
        public long channel_layout;
        public int fmt;
    };

    /// <summary>
    /// Basic audio parameters 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class SubtitleParams
    {
        public int reserve;
    };

    /// <summary>
    /// Basic media parameters
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class OpenCallbackContext
    {
        public long duration;
        public IntPtr videoParams;
        public IntPtr audioParams;
        public IntPtr subtitleParams;
        public bool realtime;
        public bool localfile;
    };

    /// <summary>
    /// This parameter will be used after audioplayer is successfully opened
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public class PlayerParams
    {
        /// <summary>
        /// Media audio parameters 
        /// </summary>
        public IntPtr srcap;

        /// <summary>
        /// In some special cases, the device does not support the audio format of the media, 
        /// so you need to change the audio output format
        /// </summary>
        public IntPtr dstap;

        /// <summary>
        /// hardware buffer size
        /// </summary>
        public int hwSize;
    }

    /// <summary>
    /// video, audio
    /// </summary>
    public enum FrameType
    {
        Video = 0,
        Audio = 1,
        Subtitle = 3
    }

    /// <summary>
    /// The meaning of context_type extension of SCFrame structure
    /// </summary>
    public enum FrameContextType
    {
        FRAME = 0,
        EOF             //Everything in this frame is invalid, to the end of the video
    }

    /// <summary>
    /// An important structure 
    /// You can get useful data from this structure 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SCFrame
    {
        public int media_type;          //For details, please see FrameType
        public int context_type;        //For details, please see FrameContextType
        public int width;               //frame pixel width
        public int height;              //frame pixel height
        public int width_ps;            //frame physics width (calculate aspect ratio)
        public int height_ps;           //frame physics height (calculate aspect ratio)
        public double rotation;         //frame rotation
        public int format;              //frame format
        public int color_range;         //color range
        public int color_space;         //color space
        public long pts;                //pts of media files 
        public long pts_ms;             //The pts of media files are based on physical time
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public int[] linesize;          //line size, and width are not necessarily the same
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public IntPtr[] data;           //data pointer
        public double duration;         //duration

        public int nb_samples;          //number of audio samples (per channel) described by this frame
        public int sample_rate;         //sample rate

        public IntPtr frame;          //avframe
        public IntPtr subtitle;
        public int uploaded;
        public IntPtr fn;
        public int flag;
        public IntPtr hwctx;
    }

    /// <summary>
    /// capture open result
    /// </summary>
    public enum CaptureOpenResult
    {
        SUCCESS = 0,
        CERTIFICATE_INVALID = -1,
        PARAMETERS_ERROR = -2,
        FAILED = -3,
    }

    public enum DeviceType
    {
        VideoInput = 0,
        AudioInput
    }

    /// <summary>
    /// audio player open result
    /// </summary>
    public enum AudioPlayerOpenResult
    {
        SUCCESS = 0,
        DeviceError = -1,
        FormatNotSupport = -2
    }

    /// <summary>
    /// resample struct
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ResampleData
    {
        public int length;
        public int nbSamples;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public IntPtr[] data;
    }

    /// <summary>
    /// resample struct
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct SoundData
    {
        public int length;
        public IntPtr data;
    };

    public enum AudioDriverType
    {
        Auto,
        Wasapi,
        DSound,
        Winmm
    }

    public enum EventCoreType
    {
        Open = 1,
        Interrupt = 2,
    }

    /// <summary>
    /// EventContext struct
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct EventContext
    {
        public int type;
        public IntPtr p1;
        public IntPtr p2;
        public IntPtr p3;
    };

    public enum EventAudioType
    {
        Open = 1,
    }
    public class ISCNative
    {
        public const string SCCore = "sccore";

        public const string SCUtility = "sccore";

        public const string AudioPlayer = "sccore";


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CamInfoCallbackDelegate(IntPtr user, IntPtr info);

        //**********************************************************************************************
        //                                  SCUtility
        //**********************************************************************************************

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SCLog(int level, byte[] log);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PeekSCLog(ref int level);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetTimestampUTC();

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern long GetTimestamp();

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SleepMs(int ms);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Memset(IntPtr dst, byte val, int len);

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Memcopy(IntPtr dst, IntPtr src, int len);

        //**********************************************************************************************
        //                                  SCCore
        //**********************************************************************************************

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr  GetJavaVM(ref int version);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr StreamCaptureProVersion();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void InitializeStreamCapturePro();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TerminateStreamCapturePro();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateStreamCapture();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseStreamCapture(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AsyncOpenStreamCapture(IntPtr capture, IntPtr url, IntPtr configuration);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseStreamCapture(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetOpenOptions(IntPtr capture, IntPtr option);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PeekCoreEvent(IntPtr capture, ref IntPtr ec);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetCaptureLoop(IntPtr capture, int loop);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SeekFastPercent(IntPtr capture, double percent);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SeekFastMs(IntPtr capture, int ms);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TryGrabFrame(IntPtr capture, int type, ref IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TryGrabLastFrame(IntPtr capture, int type, ref IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TryGrabNextFrame(IntPtr capture, int type, ref IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetStreamIndex(IntPtr capture, int type, int index);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetStreamIndex(IntPtr capture, int type, ref int index);

        //[DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        //public static extern void ClearAllCache(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FrameMoveToLast(IntPtr capture, int type);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateSCFrame(IntPtr src, int align, int flags);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseSCFrame(IntPtr src);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ImageCopy(IntPtr dst, IntPtr src);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr MemoryAlignment(IntPtr srcdata, int linepixelsize, int height, int linesize, byte[] destdata);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr FrameMoveRef(IntPtr dst, IntPtr src);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetMediaInfo(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetSubtitle(IntPtr capture, IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetPFCount(IntPtr capture, int type, ref int packetCount, ref int frameCount);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateFrameConvert();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseFrameConvert(IntPtr convert);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ConvertFrame(IntPtr convert, IntPtr scframe, int format);

        //**********************************************************************************************
        //                                  Codec
        //**********************************************************************************************

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateStreamCodec();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseStreamCodec(IntPtr codec);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenStreamCodec(IntPtr capture, IntPtr configuration);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseStreamCodec(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TryGrabStreamCodecFrame(IntPtr capture, ref IntPtr frame);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StreamCodecFrameMoveToLast(IntPtr capture);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PushToStreamCodec(IntPtr capture, IntPtr data, int size);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FlushStreamCodec(IntPtr capture);


        //**********************************************************************************************
        //                                  Device
        //**********************************************************************************************

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCameraCount();

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetCameraName(int index, int raw);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetCameraInfomation(byte[] camname, IntPtr user, CamInfoCallbackDelegate cb, int async);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMaxDisplayFrequency();

        //**********************************************************************************************
        //                                  AudioPlayer
        //**********************************************************************************************

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetAudioDriver(int audioDriver);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateAudioPlayer();

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseAudioPlayer(IntPtr player);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void AsyncOpenAudioPlayer(IntPtr player, IntPtr device, int format, IntPtr inputAS, int paused);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetAudioPlayerSem(IntPtr player);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int WaitInternalSemTimeout(IntPtr sem, int timeout, ref IntPtr buffer, ref int length);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PostExternalSem(IntPtr sem);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseAudioPlayer(IntPtr player);

        [DllImport(SCCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PeekAudioEvent(IntPtr capture, ref IntPtr ec);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PausedAudioPlayer(IntPtr player, int isPaused);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void MixAudioFormat(IntPtr stream, IntPtr src, int len, float srcVolume, float dstVolume, int format);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetBytesPerSample(int format);

        //**********************************************************************************************
        //                                  Resampler
        //**********************************************************************************************
        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateResampler();

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseResampler(IntPtr resampler);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenResampler(IntPtr resampler, IntPtr srcap, IntPtr dstap);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseResampler(IntPtr resampler);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResamplePush(IntPtr resampler, IntPtr[] data, int nbSamples);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ResampleGet(IntPtr resampler, int nbSamples);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ResamplePop(IntPtr resampler, int nbSamples);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ResampleTempo(IntPtr resampler, IntPtr data, int nbSamples, float tempo);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetUnprocessedSamples(IntPtr resampler);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResampleClear(IntPtr resampler);

        //=====================================================================
        // ByteArray
        //=====================================================================
        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateByteArray();

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ReleaseByteArray(IntPtr arr);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetByteArraySize(IntPtr arr);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetByteArrayData(IntPtr arr);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PushDataToByteArray(IntPtr arr, IntPtr data, int len);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void RemoveRangeFromByteArray(IntPtr arr, int len);

        [DllImport(AudioPlayer, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearByteArray(IntPtr arr);

        //=====================================================================
        // SCThread
        //=====================================================================

        [DllImport(SCUtility, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SCThread_WorkerCount();

        public const int FINISH_FLG = 0xFFFF;
        public const int NO_ARG = 0x0001;
        public const int HAS_ARG = 0x0002;
        public const int OPT_STRING = 0x0002;
        public const int OPT_INT = 0x0004;
        public const int OPT_FLOAT = 0x008;
        public class SCOption
        {
            public SCOption(string name, int flag, string value)
            {
                this.name = name;
                this.flag = flag;
                this.value = value;
            }
            public string name;
            public int flag;
            public string value;
        };
        public static int GetOptions(string inArgs, SCOption[] options, List<SCOption> outOptions)
        {
            outOptions.Clear();
            string[] result = inArgs.Split(';');
            if (result.Length <= 0) return 0;
            foreach (var item in result)
            {
                string str = item.Replace('\t', ' ');
                string[] group = str.Split(' ');
                string optname = group[0].Trim();
                SCOption o = find_option(options, optname);
                if (o == null)
                    continue;
                if (o.flag == HAS_ARG && group.Length < 2)
                    continue;
                if (o.flag == HAS_ARG)
                    outOptions.Add(new SCOption(o.name, o.flag, group[1].Trim()));
                else if (o.flag == NO_ARG)
                    outOptions.Add(new SCOption(o.name, o.flag, ""));
            }
            return outOptions.Count;
        }

        private static SCOption find_option(SCOption[] po, string name)
        {
            SCOption opt = null;
            for (int i = 0; i < po.Length; i++)
            {
                if (name == po[i].name)
                {
                    opt = po[i];
                    break;
                }
            }
            return opt;
        }

        public static void SCLog(LogLevel level, string log)
        {
            SCLog((int)level, StringToByteArray(log));
        }
        public static byte[] StringToByteArray(string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str).Concat(new byte[1] { 0 }).ToArray();
        }

        public static IntPtr StructureToIntPtr(object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structObj, structPtr, false);
            return structPtr;
        }

        public static void ReleaseStructIntPtr(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        public static IntPtr StringToIntPtr(string str)
        {
            byte[] buff = StringToByteArray(str);
            IntPtr ptr = Marshal.AllocHGlobal(buff.Length);
            Marshal.Copy(buff, 0, ptr, buff.Length);
            return ptr;
        }

        public static void ReleaseStringIntPtr(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public interface ISCPlayerPro
    {
        void Update();
    }
}