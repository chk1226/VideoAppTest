using System;

namespace Sttplay.MediaPlayer
{
    /// <summary>
    /// A relatively accurate clock
    /// The clock is used for audio and video synchronization 
    /// This clock is not very useful for users
    /// </summary>
    public class SCClock
    {
        private double pts;
        private double pts_drift;
        private bool paused;

        private const int AV_NOSYNC_THRESHOLD = 25;

        public SCClock()
        {
            SetClock(double.NaN);
            paused = false;
        }

        public void SetClockAt(double pts, double time)
        {
            this.pts = pts;
            this.pts_drift = pts - time;
        }

        public void SetClock(double crtts)
        {
            //utc timestamp
            double time = ISCNative.GetTimestampUTC() / 1000.0;
            SetClockAt(crtts, time);
        }

        public double GetClock()
        {
            if (paused)
            {
                return pts;
            }
            else
            {
                double time = ISCNative.GetTimestampUTC() / 1000.0;
                return pts_drift + time;
            }
        }

        public void SyncClockToSlave(SCClock slave, bool force)
        {
            double clock = GetClock();
            double slave_clock = slave.GetClock();
            if (!double.IsNaN(slave_clock) && (double.IsNaN(clock) || Math.Abs((float)(clock - slave_clock)) > AV_NOSYNC_THRESHOLD) || force)
            {
                SetClock(slave_clock);
            }
        }
    }
}