using System;

namespace RemoteTechWormholeBridge.Core.Contracts
{
    public enum ServiceTimerTransition
    {
        None,
        Started,
        Reset,
        Completed
    }

    public sealed class ContinuousServiceTimer
    {
        private double startedAt;
        private bool completed;

        public ContinuousServiceTimer(double duration)
        {
            if (Double.IsNaN(duration) || Double.IsInfinity(duration) || duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration));

            Duration = duration;
            startedAt = Double.NaN;
        }

        public double Duration { get; private set; }
        public bool IsRunning { get { return !Double.IsNaN(startedAt) && !completed; } }
        public bool IsComplete { get { return completed; } }
        public double StartedAt { get { return startedAt; } }

        public double Elapsed(double universalTime)
        {
            if (completed)
                return Duration;
            if (!IsRunning || !IsFinite(universalTime) || universalTime < startedAt)
                return 0;
            return Math.Min(Duration, universalTime - startedAt);
        }

        public ServiceTimerTransition Update(double universalTime, bool serviceAvailable)
        {
            if (completed)
                return ServiceTimerTransition.None;
            if (!IsFinite(universalTime))
                return Reset();

            if (!serviceAvailable)
                return Reset();

            if (!IsRunning || universalTime < startedAt)
            {
                startedAt = universalTime;
                return ServiceTimerTransition.Started;
            }

            if (universalTime - startedAt >= Duration)
            {
                completed = true;
                return ServiceTimerTransition.Completed;
            }

            return ServiceTimerTransition.None;
        }

        public void Restore(double savedStartedAt, bool savedCompleted)
        {
            completed = savedCompleted;
            startedAt = savedCompleted || !IsFinite(savedStartedAt) ? Double.NaN : savedStartedAt;
        }

        private ServiceTimerTransition Reset()
        {
            if (!IsRunning)
                return ServiceTimerTransition.None;
            startedAt = Double.NaN;
            return ServiceTimerTransition.Reset;
        }

        private static bool IsFinite(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }
    }
}
