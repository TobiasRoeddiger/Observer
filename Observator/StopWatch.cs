using System;

namespace Observator
{
    class StopWatch
    {
        public DateTime startTime;
        DateTime startPauseTime;
        TimeSpan pausedTime = TimeSpan.Zero;

        public StopWatch()
        {
            startTime = DateTime.Now;
        }

        public void Pause()
        {
            startPauseTime = DateTime.Now;
        }

        public void Resume()
        {
            pausedTime = DateTime.Now.Subtract(startPauseTime).Add(pausedTime);
        }

        public TimeSpan getTimeDifference()
        {
            return DateTime.Now.Subtract(startTime).Subtract(pausedTime);
        }
    }
}
