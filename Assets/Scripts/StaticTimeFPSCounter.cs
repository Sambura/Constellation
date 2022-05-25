using System.Collections.Generic;
using UnityEngine;

public class StaticTimeFPSCounter : MonoBehaviour
{
    [SerializeField] private float _timeWindow = 5f;
    [SerializeField] private int _initialBufferCapacity = 600;

    private Queue<(float, float)> _frameTimings;
    private float _currentTime;

	public float CurrentFps => _frameTimings.Count / _currentTime; //  ==  1 / (_currentTime / _frameBufferCapacity)

	void Start()
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
