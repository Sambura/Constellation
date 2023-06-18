using System.Collections.Generic;
using UnityEngine;

namespace UnityCore
{
    public class StaticTimeFPSCounter : MonoBehaviour
    {
        [SerializeField] private float _timeWindow = 5f;
        [SerializeField] private int _initialBufferCapacity = 600;

        private Queue<(float, float)> _frameTimings;
        private float _currentTime;

        public float TimeWindow { get => _timeWindow; set => _timeWindow = value; }
        public float CurrentFps => _frameTimings.Count / _currentTime;

        void Awake()
        {
            _frameTimings = new Queue<(float, float)>(_initialBufferCapacity);
        }

        void Update()
        {
            float currentTime = Time.time;
            float currentDelta = Time.deltaTime;
            float oldestAllowed = currentTime - _timeWindow;

            while (_frameTimings.Count > 0 && _frameTimings.Peek().Item1 < oldestAllowed)
                _currentTime -= _frameTimings.Dequeue().Item2;

            _currentTime += currentDelta;
            _frameTimings.Enqueue((currentTime, currentDelta));
        }
    }
}