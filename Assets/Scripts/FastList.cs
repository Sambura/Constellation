namespace Assets.Scripts
{
    /// <summary>
    /// Replacement for System.Collections.Generic.List
    /// Has less restrictive interface and should be more performant overall
    /// </summary>
    public class FastList<T>
    {
        private T[] _buffer;
        
        /// <summary>
        /// Changing this property does not clear/expand the list
        /// </summary>
        public int Count { get => _count; set => _count = value; }
        public int Capacity => _buffer.Length;

        private int _count;
        private int _capacity;

        /// <summary>
        /// List indexer.
        /// You are allowed to index any item from the internal buffer.
        /// </summary>
		public T this[int index]
		{
            get => _buffer[index];
            set => _buffer[index] = value;
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
            System.Array.Copy(list._buffer, 0, _buffer, _count, list._count);
            _count = combined;
		}

        public FastList(int capacity = 8)
        {
            if (capacity <= 0) capacity = 1;
            _capacity = capacity;
            _buffer = new T[capacity];
        }
    }
}