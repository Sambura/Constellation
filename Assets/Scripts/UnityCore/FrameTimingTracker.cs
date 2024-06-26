using UnityEngine;
using System;

namespace UnityCore
{
	public sealed class FrameTimingTracker : MonoBehaviour
	{
		private float[] _timings;
		private bool _enabled;
		private int _index;

		/// <summary>
		/// The buffer with captured frame timings in seconds
		/// </summary>
		public float[] Buffer => _timings;
		public int FramesCaptured => _index;

		/// <summary>
		/// The buffer size for frame timings. Setting this value allocates a new array,
		/// and the old data is not transferred to it
		/// </summary>
		public int BufferSize { 
			get => _timings?.Length ?? -1;
			set
			{
				if (value == BufferSize) return;
				_timings = new float[value];
			}
		}

		public bool ValidState => _index <= BufferSize;

		/// <summary>
		/// Invoked whenever PrepareTracking() is called. Can be used to prepare external
		/// objects for tracking.
		/// </summary>
		public event Action OnPrepare;

		public void PrepareTracking()
		{
			OnPrepare?.Invoke();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		public void StartTracking()
		{
			if (BufferSize < 0) throw new InvalidOperationException("Cannot start tracking without buffer size");
			_index = 0;
			enabled = _enabled = true;
		}

		public void StopTracking()
		{
			enabled = _enabled = false;
		}

		private void Awake()
		{
			enabled = _enabled;
		}

		// If this can be improved, I don't mind...
		private void Update()
		{
			_timings[_index] = Time.unscaledDeltaTime;
			_index++;
		}
	}
}
