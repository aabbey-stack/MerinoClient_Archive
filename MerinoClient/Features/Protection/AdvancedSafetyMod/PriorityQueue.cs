using System.Collections.Generic;

namespace MerinoClient.Features.Protection.AdvancedSafetyMod;

internal class PriorityQueue<T>
{
    private readonly List<T> _myBackingStorage;
    private readonly IComparer<T> _myComparer;

    public PriorityQueue(IComparer<T> comparer)
    {
        _myComparer = comparer;
        _myBackingStorage = new List<T>();
    }

    public int Count => _myBackingStorage.Count;

    public void Enqueue(T value)
    {
        _myBackingStorage.Add(value);
        SiftUp(_myBackingStorage.Count - 1);
    }

    public T Dequeue()
    {
        if (_myBackingStorage.Count == 0)
            return default;
        Swap(0, _myBackingStorage.Count - 1);
        var result = _myBackingStorage[_myBackingStorage.Count - 1];
        _myBackingStorage.RemoveAt(_myBackingStorage.Count - 1);
        SiftDown(0);
        return result;
    }

    private void Swap(int i1, int i2)
    {
        var value1 = _myBackingStorage[i1];
        var value2 = _myBackingStorage[i2];
        _myBackingStorage[i1] = value2;
        _myBackingStorage[i2] = value1;
    }

    private void SiftDown(int i)
    {
        var childIndex1 = i * 2 + 1;
        var childIndex2 = i * 2 + 2;
        if (childIndex1 >= _myBackingStorage.Count)
            return;
        var child1 = _myBackingStorage[childIndex1];
        if (childIndex2 >= _myBackingStorage.Count)
        {
            var compared = _myComparer.Compare(_myBackingStorage[i], child1);
            if (compared > 0) Swap(i, childIndex1);
            return;
        }

        var child2 = _myBackingStorage[childIndex2];
        var compared1 = _myComparer.Compare(_myBackingStorage[i], child1);
        var compared2 = _myComparer.Compare(_myBackingStorage[i], child2);
        if (compared1 > 0 || compared2 > 0)
        {
            var compared12 = _myComparer.Compare(child1, child2);
            if (compared12 > 0)
            {
                Swap(i, childIndex2);
                SiftDown(childIndex2);
            }
            else
            {
                Swap(i, childIndex1);
                SiftDown(childIndex1);
            }
        }
    }

    private void SiftUp(int i)
    {
        if (i == 0)
            return;
        var parentIndex = (i - 1) / 2;
        var compared = _myComparer.Compare(_myBackingStorage[i], _myBackingStorage[parentIndex]);
        if (compared < 0)
        {
            Swap(i, parentIndex);
            SiftUp(parentIndex);
        }
    }
}