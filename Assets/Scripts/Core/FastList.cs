using System;
namespace Core
{
    /// <summary>
    /// Replacement for System.Collections.Generic.List
    /// Has less restrictive interface and should be more performant overall
    /// Can be useful when there is a need to frequently clear the list and fill it again
    /// </summary>
    public class FastList<T>
    {
        public T[] _buffer;

        /// <summary>
        /// Changing this property does not clear/expand the list
        /// </summary>
        public int Capacity => _buffer.Length;

        public int _count;
        private int _capacity;

        /// <summary>
        /// List indexer.
        /// You are allowed to index any item from the internal buffer.
        /// </summary>
		public ref T this[int index]
        {
            get { return ref _buffer[index]; }
        }

        private void DoubleCapacity()
        {
            _capacity *= 2;
            T[] newBuffer = new T[_capacity];
            _buffer.CopyTo(newBuffer, 0);
            _buffer = newBuffer;
        }

        public void Add(T item)
        {
            if (_count == _capacity) DoubleCapacity();
            _buffer[_count] = item;
            _count++;
        }

        public void AddRange(FastList<T> list)
        {
            int combined = _count + list._count;
            if (combined > _capacity)
            {
                _capacity = combined;
                T[] newBuffer = new T[_capacity];
                _buffer.CopyTo(newBuffer, 0);
                _buffer = newBuffer;
            }
            Array.Copy(list._buffer, 0, _buffer, _count, list._count);
            _count = combined;
        }

        public void PseudoClear() => _count = 0;

        public T[] ToArray()
		{
            T[] array = new T[_count];
            Array.Copy(_buffer, 0, array, 0, _count);
            return array;
		}

        public FastList(int capacity = 8)
        {
            if (capacity <= 0) capacity = 1;
            _capacity = capacity;
            _buffer = new T[capacity];
        }
    }
}