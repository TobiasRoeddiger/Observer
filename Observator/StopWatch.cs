using System;

namespace Observator
{
    class StopWatch
    {
        public DateTime startTime;

        public StopWatch()
        {
            startTime = DateTime.Now;
        }

        public TimeSpan getTimeDifference()
        {
            return DateTime.Now.Subtract(startTime);
        }
    }
}
