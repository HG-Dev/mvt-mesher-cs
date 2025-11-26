
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MvtMesherCore.Collections;

public abstract class ReadOnlyKeyedBucketCollection<TKey, TKeyedValue>
    : IReadOnlyDictionary<TKey, IEnumerable<TKeyedValue>>, IEqualityComparer<TKey>
    where TKey : notnull
    where TKeyedValue : notnull
{
    protected ReadOnlyKeyedBucketCollection(IEnumerable<TKeyedValue> features, IComparer<TKey>? keyComparer = null)
    {
        KeyComparer = keyComparer ?? Comparer<TKey>.Default;
        var groups = features.OrderBy(GetKey, KeyComparer);
        Items = groups.ToArray();
        KeyCount = Items.Length;
        ItemCount = Items.Length;
    }

    public readonly IComparer<TKey> KeyComparer;
    protected abstract TKey GetKey(TKeyedValue item);

    // Items are stored sorted by key for efficient lookup
    protected readonly TKeyedValue[] Items;
    public int Count => KeyCount;
    public readonly int KeyCount;
    public readonly int ItemCount;

    protected bool KeyEquals(TKey a, TKey b) => KeyComparer.Compare(a, b) == 0;
    public IEnumerable<TKey> Keys => Items.Select(GetKey).Distinct(this);

    public IEnumerable<IEnumerable<TKeyedValue>> Values
    {
        get
        {
            var groups = Items.GroupBy(GetKey);
            return groups;
        }
    }

    public IEnumerator<KeyValuePair<TKey, IEnumerable<TKeyedValue>>> GetEnumerator()
    {
        var groups = Items.GroupBy(GetKey);
        foreach (var group in groups)
        {
            yield return new KeyValuePair<TKey, IEnumerable<TKeyedValue>>(group.Key, group);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsKey(TKey key) =>
        Items.Any(i => KeyEquals(GetKey(i), key));

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out IEnumerable<TKeyedValue> value)
    {
        var startIndex = -1;
        var endIndex = -1;
        for (int i = 0; i < Items.Length; i++)
        {
            if (KeyEquals(GetKey(Items[i]), key))
            {
                startIndex = startIndex < 0 ? i : startIndex;
                endIndex = i; 
            }
            else if (startIndex >= 0)
            {
                // All matching items have been found
                value = Items[startIndex..(endIndex + 1)];
                return true;
            }
        }

        value = default!;
        return false;
    }

    public IEnumerable<TKeyedValue> this[TKey key] => TryGetValue(key, out var value) ? value : Array.Empty<TKeyedValue>();

    bool IEqualityComparer<TKey>.Equals(TKey x, TKey y) => KeyComparer.Compare(x, y) == 0;

    int IEqualityComparer<TKey>.GetHashCode(TKey obj) => obj.GetHashCode();
}
