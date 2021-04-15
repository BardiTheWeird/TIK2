using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    public class Heap<T>
    {
        private readonly T[] _elements;
        public int Size { get; set; }

        private Func<T, T, bool> _comparer;

        public Heap(int size, Func<T, T, bool> comparer)
        {
            _elements = new T[size];
            _comparer = comparer;
        }

        private int GetLeftChildIndex(int elementIndex) => 2 * elementIndex + 1;
        private int GetRightChildIndex(int elementIndex) => 2 * elementIndex + 2;
        private int GetParentIndex(int elementIndex) => (elementIndex - 1) / 2;

        private bool HasLeftChild(int elementIndex) => GetLeftChildIndex(elementIndex) < Size;
        private bool HasRightChild(int elementIndex) => GetRightChildIndex(elementIndex) < Size;
        private bool IsRoot(int elementIndex) => elementIndex == 0;

        private T GetLeftChild(int elementIndex) => _elements[GetLeftChildIndex(elementIndex)];
        private T GetRightChild(int elementIndex) => _elements[GetRightChildIndex(elementIndex)];
        private T GetParent(int elementIndex) => _elements[GetParentIndex(elementIndex)];

        private void Swap(int firstIndex, int secondIndex)
        {
            var temp = _elements[firstIndex];
            _elements[firstIndex] = _elements[secondIndex];
            _elements[secondIndex] = temp;
        }

        public bool IsEmpty() => Size == 0;

        public T Peek()
        {
            if (Size == 0)
                throw new IndexOutOfRangeException();

            return _elements[0];
        }

        public T Pop()
        {
            if (Size == 0)
                throw new IndexOutOfRangeException();

            var result = _elements[0];
            _elements[0] = _elements[Size - 1];
            Size--;

            ReCalculateDown();

            return result;
        }

        public void Add(T element)
        {
            if (Size == _elements.Length)
                throw new IndexOutOfRangeException();

            _elements[Size] = element;
            Size++;

            ReCalculateUp();
        }

        private void ReCalculateDown()
        {
            int index = 0;
            while (HasLeftChild(index))
            {
                var smallerIndex = GetLeftChildIndex(index);
                if (HasRightChild(index) && _comparer(GetRightChild(index), GetLeftChild(index)))
                    smallerIndex = GetRightChildIndex(index);

                if (!_comparer(_elements[smallerIndex], _elements[index]))
                    break;

                Swap(smallerIndex, index);
                index = smallerIndex;
            }
        }

        private void ReCalculateUp()
        {
            var index = Size - 1;
            while (!IsRoot(index) && _comparer(_elements[index], GetParent(index)))
            {
                var parentIndex = GetParentIndex(index);
                Swap(parentIndex, index);
                index = parentIndex;
            }
        }
    }
}
