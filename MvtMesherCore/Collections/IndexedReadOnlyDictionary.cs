
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a read-only dictionary that references external key and value lists.
/// Pairs are stored as integer indices pointing to these lists and sorted by key index.
/// Implements IReadOnlyDictionary<TKey, TValue>.
/// </summary>
/// <typeparam name="TKey">The type of keys.</typeparam>
/// <typeparam name="TValue">The type of values.</typeparam>
public sealed class IndexedReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
    where TValue : notnull
{
    readonly IReadOnlyList<TKey> _keys;
    readonly IReadOnlyList<TValue> _values;
    readonly (int keyIndex, int valueIndex)[] _pairs;
    readonly Dictionary<TKey, int> _lazyKeyIndexMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedReadOnlyDictionary{TKey,TValue}"/> class.
    /// </summary>
    /// <param name="keys">The list of keys referenced by indices.</param>
    /// <param name="values">The list of values referenced by indices.</param>
    /// <param name="pairs">
    /// The collection of key-value index pairs.
    /// Can be null, in which case an empty array is created.
    /// </param>
    public IndexedReadOnlyDictionary(
        IReadOnlyList<TKey> keys,
        IReadOnlyList<TValue> values,
        IEnumerable<(int keyIndex, int valueIndex)>? pairs)
    {
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _values = values ?? throw new ArgumentNullException(nameof(values));

        _pairs = (pairs ?? []).ToArray();
        Array.Sort(_pairs, CompareKeyIndices);

        // Sort pairs by keyIndex for binary search
        for (int i = 1; i < _pairs.Length; i++)
        {
            if (CompareKeyIndices(_pairs[i], _pairs[i - 1]) is 0)
                throw new ArgumentException($"Encountered duplicate key index {_pairs[i].keyIndex}. Context: {_pairs[i - 1]}@{i-1} vs. {_pairs[i]}@{i}");
        }

        // Prepare key index map for O(1) lookup
        _lazyKeyIndexMap = new Dictionary<TKey, int>(keys.Count);
    }

    public IndexedReadOnlyDictionary(IReadOnlyList<TKey> keys, IReadOnlyList<TValue> values, IReadOnlyList<int> pairStream) : this(keys, values,
        BufferPairStream(pairStream))
    {
    }
    
    static int CompareKeyIndices((int keyIndex, int valueIndex) a, (int keyIndex, int valueIndex) b) => a.keyIndex.CompareTo(b.keyIndex);

    static IEnumerable<(int, int)> BufferPairStream(IReadOnlyList<int> pairStream)
    {
        if (pairStream.Count % 2 is not 0)
        {
            throw new ArgumentException("Odd number of indices in pair stream");
        }
        for (int i = 1; i < pairStream.Count; i += 2)
        {
            var vIdx = pairStream[i];
            var kIdx = pairStream[i-1];
            yield return (kIdx, vIdx);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool TryGetKeyIndex(TKey key, out int keyIndex)
    {
        if (!_lazyKeyIndexMap.TryGetValue(key, out keyIndex))
        {
            for (int i = 0; i < _keys.Count; i++)
            {
                if (!EqualityComparer<TKey>.Default.Equals(key, _keys[i])) 
                    continue;
                
                keyIndex = i;
                break;
            }

            if (keyIndex < 0)
            {
                return false;
            }

            _lazyKeyIndexMap.Add(key, keyIndex);
        }

        return true;
    }
    
    /// <inheritdoc />
    public bool ContainsKey(TKey key)
    {
        if (!TryGetKeyIndex(key, out int keyIndex))
        {
            return false;
        }

        return BinarySearchPairs(keyIndex) >= 0;
    }

    /// <inheritdoc />
    public bool TryGetValue(TKey key, out TValue value)
    {
        value = default!;
        if (!TryGetKeyIndex(key, out int keyIndex))
        {
            return false;
        }

        int pairIndex = BinarySearchPairs(keyIndex);
        if (pairIndex < 0)
            return false;

        value = _values[_pairs[pairIndex].valueIndex];
        return true;
    }

    /// <inheritdoc />
    public TValue this[TKey key]
    {
        get
        {
            if (!TryGetValue(key, out var value))
                throw new KeyNotFoundException();
            return value;
        }
    }

    /// <inheritdoc />
    public IEnumerable<TKey> Keys => _pairs.Select(p => _keys[p.keyIndex]);

    /// <inheritdoc />
    public IEnumerable<TValue> Values => _pairs.Select(p => _values[p.valueIndex]);

    /// <inheritdoc />
    public int Count => _pairs.Length;

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var (kIdx, vIdx) in _pairs)
            yield return new KeyValuePair<TKey, TValue>(_keys[kIdx], _values[vIdx]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Performs a binary search on the sorted pairs array to find the given key index.
    /// </summary>
    /// <param name="keyIndex">The key index to search for.</param>
    /// <returns>The index of the pair if found; otherwise, -1.</returns>
    int BinarySearchPairs(int keyIndex)
    {
        int low = 0, high = _pairs.Length - 1;
        while (low <= high)
        {
            // Get the midpoint index as mid
            int mid = (low + high) >> 1;
            switch (_pairs[mid].keyIndex.CompareTo(keyIndex))
            {
                case 0: // Our desired keyIndex was found
                    return mid;
                case < 0: // keyIndex is bigger than what was found at mid
                    low = mid + 1;
                    continue;
                default: // keyIndex is smaller than what was found at mid
                    high = mid - 1;
                    continue;
            }
        }
        // keyIndex was not found
        return -1;
    }
}
