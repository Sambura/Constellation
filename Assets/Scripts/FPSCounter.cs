using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private int frameBufferCapacity;

    private Queue<float> _frameTimings;
    private float _currentTime;
    private float _frameBufferCapacity;
    private int _framesCount;

	public float CurrentFps => _frameBufferCapacity / _currentTime; //  ==  1 / (_currentTime / _frameBufferCapacity)
    public bool IsValid => _framesCount >= frameBufferCapacity;

	void Start()
    {
        _frameTimings = new Queue<float>(frameBufferCapacity);
        for (int i = 0; i < frameBufferCapacity; i++)
            _frameTimings.Enqueue(0);
        _frameBufferCapacity = frameBufferCapacity;
    }

    void Update()
    {
        _framesCount++;
        _currentTime += Time.deltaTime;

        _currentTime -= _frameTimings.Dequeue();
        _frameTimings.Enqueue(Time.deltaTime);
    }
}
