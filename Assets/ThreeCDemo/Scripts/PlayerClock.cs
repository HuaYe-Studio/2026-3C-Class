using UnityEngine;

namespace ThreeCDemo
{
    public interface IClock
    {
        double Now { get; }
        double Realtime { get; }
        float DeltaTime { get; }
    }

    public sealed class UnityClock : IClock
    {
        public double Now => Time.timeAsDouble;
        public double Realtime => Time.realtimeSinceStartupAsDouble;
        public float DeltaTime => Time.deltaTime;
    }
}
