using System.Collections.Generic;
using UnityEngine;

namespace UnityCore
{
    /// <summary>
    /// This component allows to easily get a current FPS reading with the ability to adjust
    /// time window. The component should have a negligible effect on performance. If the 
    /// component gets disabled, it should no longer consume ANY cpu time, however FPS reading
    /// will stop updating as well
    /// </summary>
    public class StaticTimeFPSCounter : MonoBehaviour
    {
        [SerializeField] private float _timeWindow = 5f;
        [SerializeField] private int _initialBufferCapacity = 600;

        private Queue<(float, float)> _frameTimings;
        private float _currentTime;

        public float TimeWindow { get => _timeWindow; set => _timeWindow = value; }
        public float CurrentFps => _frameTimings.Count / _currentTime;
        public float CapturedTimeRange => Time.time - _frameTimings.Peek().Item1;

        private void Awake()
        {
            _frameTimings = new Queue<(float, float)>(_initialBufferCapacity);
        }

        private void Update()
        {
            float currentTime = Time.time;
            float currentDelta = Time.unscaledDeltaTime;
            float oldestAllowed = currentTime - _timeWindow;

            while (_frameTimings.Count > 0 && _frameTimings.Peek().Item1 < oldestAllowed)
                _currentTime -= _frameTimings.Dequeue().Item2;

            _currentTime += currentDelta;
            _frameTimings.Enqueue((currentTime, currentDelta));
        }
    }
}