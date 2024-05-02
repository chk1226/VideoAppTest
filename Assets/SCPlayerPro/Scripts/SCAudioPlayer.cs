using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sttplay.MediaPlayer
{
    public class SCAudioPlayer : ISCPlayerPro
    {

        public float Volume { get; set; }
        public bool AutoJustSpeed { get; set; }
        public bool FirstDelay { get; set; }

        private float ovolume;
        private float speed = 1.0f;
        private bool firstWait = true;
        public event Action<AudioPlayerOpenResult, string, PlayerParams> onOpenCallbackEvent;

        private AudioOutputFormat audioOutputFmt = AudioOutputFormat.FLT;

        private IntPtr resampler;
        private IntPtr aplayer;
        private IntPtr audioSem;
        private IntPtr pcm;
        private readonly Mutex mux;

        private bool Closed = true;
        private bool waitAudioPlayer = false;

        private int audioHWBufSize = 0;
        private int bytesPerSec = 0;
        private int sampleRate = 0;
        private double audioDiffThreshold;

        private Thread _audioThreadThreadHandle;

        public SCAudioPlayer()
        {
            SCMGR.AddPlayer(this);
            resampler = ISCNative.CreateResampler();
            aplayer = ISCNative.CreateAudioPlayer();
            mux = new Mutex();
            pcm = ISCNative.CreateByteArray();
            ovolume = Volume = 1.0f;
            AutoJustSpeed = true;
            FirstDelay = true;
            ovolume = Volume;
        }

        public void Release()
        {
            Close();
            if (aplayer == IntPtr.Zero) return;
            ISCNative.ReleaseAudioPlayer(aplayer);
            ISCNative.ReleaseResampler(resampler);
            ISCNative.ReleaseByteArray(pcm);
            aplayer = IntPtr.Zero;
            resampler = IntPtr.Zero;
            pcm = IntPtr.Zero;
            SCMGR.RemovePlayer(this);
        }

        private void AudioPlayerOpenCallback(AudioPlayerOpenResult code, string error, PlayerParams param)
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
            firstWait = FirstDelay;
            if (onOpenCallbackEvent != null)
                onOpenCallbackEvent(code, error, param);
        }

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

        public void AudioPlayCallback(IntPtr buffer, int len)
        {
            ISCNative.Memset(buffer, 0, len);
            float duration = ISCNative.GetByteArraySize(pcm) / (float)bytesPerSec;
            float ms = len / (float)bytesPerSec;
            if (firstWait)
            {
                if (duration < ms * 5)
                    return;
                firstWait = false;
            }
            while (!Closed && ISCNative.GetByteArraySize(pcm) < len) Thread.Sleep(1);

            int size = ISCNative.GetByteArraySize(pcm);
            int minLen = Math.Min(len, size);
            ISCNative.MixAudioFormat(buffer, ISCNative.GetByteArrayData(pcm), minLen, ovolume, Volume, (int)audioOutputFmt);
            ovolume = Volume;
            mux.WaitOne();
            ISCNative.RemoveRangeFromByteArray(pcm, minLen);
            mux.ReleaseMutex();

            duration = ISCNative.GetByteArraySize(pcm) / (float)bytesPerSec;
            if (AutoJustSpeed)
            {
                if (duration > ms * 5)
                    speed = 1.1f;
                else if (duration > ms * 3)
                    speed = 1.01f;
                else if (duration < ms * 2)
                    speed = 0.99f;
                else
                    speed = 1.0f;
            }
        }

        public void AsyncOpen(AudioParams ap)
        {
            Close();
            if (aplayer == IntPtr.Zero) return;
            ovolume = Volume;
            waitAudioPlayer = true;
            Closed = false;
            IntPtr apPtr = ISCNative.StructureToIntPtr(ap);
            ISCNative.AsyncOpenAudioPlayer(aplayer, IntPtr.Zero, (int)audioOutputFmt, apPtr, 0);
            ISCNative.ReleaseStructIntPtr(apPtr);
            audioSem = ISCNative.GetAudioPlayerSem(aplayer);
            if (_audioThreadThreadHandle != null)
                throw new Exception("auido thread ready exists!");
            _audioThreadThreadHandle = new Thread(AudioThread);
            _audioThreadThreadHandle.Start();
        }

        public void Close()
        {
            mux.WaitOne();
            Closed = true;
            mux.ReleaseMutex();
            if (aplayer == IntPtr.Zero) return;
            ISCNative.CloseAudioPlayer(aplayer);
            if (_audioThreadThreadHandle != null)
            {
                _audioThreadThreadHandle.Join();
                _audioThreadThreadHandle = null;
            }
            ISCNative.ClearByteArray(pcm);
            ISCNative.ResampleClear(resampler);
        }

        public void Update()
        {
            if (Closed) return;
            IntPtr ec = IntPtr.Zero;
            if (ISCNative.PeekAudioEvent(aplayer, ref ec) >= 0)
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
        }

        public void PushFrame(SCFrame frame)
        {
            mux.WaitOne();
            if (Closed)
            {
                mux.ReleaseMutex();
                return;
            }
            ISCNative.ResamplePush(resampler, frame.data, frame.nb_samples);
            IntPtr resampleDataPtr = ISCNative.ResampleGet(resampler, -1);
            ResampleData rd = Marshal.PtrToStructure<ResampleData>(resampleDataPtr);
            IntPtr soundDataPtr = ISCNative.ResampleTempo(resampler, rd.data[0], rd.nbSamples, speed);
            if (soundDataPtr != IntPtr.Zero)
            {
                SoundData sd = Marshal.PtrToStructure<SoundData>(soundDataPtr);
                ISCNative.PushDataToByteArray(pcm, sd.data, sd.length);
            }
            ISCNative.ResamplePop(resampler, rd.nbSamples);
            mux.ReleaseMutex();
        }
    }
}
