using System.Threading;

namespace System.Collections.Generic
{
    public class Deque<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
    {
        private T[] _array;

        private int _head;

        private int _tail;

        private int _size;

        private object _syncRoot;

        private const int _MinimumGrow = 4;

        private const int _ShrinkThreshold = 32;

        private const int _GrowFactor = 200;

        private const int _DefaultCapacity = 4;

        private static T[] _emptyArray = new T[0];
        public int Count
        {
            get
            {
                return _size;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    Interlocked.CompareExchange<object>(ref _syncRoot, new object(), (object)null);
                }

                return _syncRoot;
            }
        }

        public Deque()
        {
            _array = _emptyArray;
        }

        public Deque(int capacity)
        {
            _array = new T[capacity];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public Deque(IEnumerable<T> collection)
        {
            _array = new T[4];
            _size = 0;
            foreach (T item in collection)
            {
                Enqueue(item);
            }
        }

        public void Clear()
        {
            if (_head < _tail)
            {
                Array.Clear(_array, _head, _size);
            }
            else
            {
                Array.Clear(_array, _head, _array.Length - _head);
                Array.Clear(_array, 0, _tail);
            }

            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int num = array.Length;

            int num2 = ((num - arrayIndex < _size) ? (num - arrayIndex) : _size);
            if (num2 != 0)
            {
                int num3 = ((_array.Length - _head < num2) ? (_array.Length - _head) : num2);
                Array.Copy(_array, _head, array, arrayIndex, num3);
                num2 -= num3;
                if (num2 > 0)
                {
                    Array.Copy(_array, 0, array, arrayIndex + _array.Length - _head, num2);
                }
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            int length = array.Length;
            int num = ((length - index < _size) ? (length - index) : _size);
            if (num == 0)
            {
                return;
            }

            int num2 = ((_array.Length - _head < num) ? (_array.Length - _head) : num);
            Array.Copy(_array, _head, array, index, num2);
            num -= num2;
            if (num > 0)
            {
                Array.Copy(_array, 0, array, index + _array.Length - _head, num);
            }
        }

        public void Enqueue(T item)
        {
            if (_size == _array.Length)
            {
                int num = (int)((long)_array.Length * 200L / 100);
                if (num < _array.Length + 4)
                {
                    num = _array.Length + 4;
                }

                SetCapacity(num);
            }

            _array[_tail] = item;
            _tail = (_tail + 1) % _array.Length;
            _size++;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public T Dequeue()
        {
            T result = _array[_head];
            _array[_head] = default(T);
            _head = (_head + 1) % _array.Length;
            _size--;
            return result;
        }

        public T Peek()
        {
            return _array[_head];
        }

        public bool Contains(T item)
        {
            int num = _head;
            int size = _size;
            EqualityComparer<T> @default = EqualityComparer<T>.Default;
            while (size-- > 0)
            {
                if (item == null)
                {
                    if (_array[num] == null)
                    {
                        return true;
                    }
                }
                else if (_array[num] != null && @default.Equals(_array[num], item))
                {
                    return true;
                }

                num = (num + 1) % _array.Length;
            }

            return false;
        }

        public T GetElement(int i) // fuck you
        {
            return _array[(_head + i) % _array.Length];
        }

        public T Head => _array[_head];
        public T Tail => _array[(_tail + _array.Length - 1) % _array.Length];

        public T[] ToArray()
        {
            T[] array = new T[_size];
            if (_size == 0)
            {
                return array;
            }

            if (_head < _tail)
            {
                Array.Copy(_array, _head, array, 0, _size);
            }
            else
            {
                Array.Copy(_array, _head, array, 0, _array.Length - _head);
                Array.Copy(_array, 0, array, _array.Length - _head, _tail);
            }

            return array;
        }

        private void SetCapacity(int capacity)
        {
            T[] array = new T[capacity];
            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, array, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, array, 0, _array.Length - _head);
                    Array.Copy(_array, 0, array, _array.Length - _head, _tail);
                }
            }

            _array = array;
            _head = 0;
            _tail = ((_size != capacity) ? _size : 0);
        }

        public void TrimExcess()
        {
            int num = (int)((double)_array.Length * 0.9);
            if (_size < num)
            {
                SetCapacity(_size);
            }
        }

        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private Deque<T> _q;

            private int _index;

            private T _currentElement;

            public T Current
            {
                get
                {
                    return _currentElement;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return _currentElement;
                }
            }

            internal Enumerator(Deque<T> q)
            {
                _q = q;
                _index = -1;
                _currentElement = default(T);
            }

            public void Dispose()
            {
                _index = -2;
                _currentElement = default(T);
            }

            public bool MoveNext()
            {
                if (_index == -2)
                {
                    return false;
                }

                _index++;
                if (_index == _q._size)
                {
                    _index = -2;
                    _currentElement = default(T);
                    return false;
                }

                _currentElement = _q.GetElement(_index);
                return true;
            }

            void IEnumerator.Reset()
            {
                _index = -1;
                _currentElement = default(T);
            }
        }
    }
}