
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MvtMesherCore.Collections;

/// <summary>
/// A read-only collection of keyed buckets, which contain one or more keyed values.
/// Keyes are unique for buckets, but not for individual values.
/// Keys are derived from the keyed values using an abstract key selector.
/// </summary>
/// <typeparam name="TKey">Non-null key type.</typeparam>
/// <typeparam name="TKeyedValue">Non-null keyed value type from which a key can be derived.</typeparam>
public abstract class ReadOnlyKeyedBucketCollection<TKey, TKeyedValue>
    : IReadOnlyDictionary<TKey, IEnumerable<TKeyedValue>>, IEqualityComparer<TKey>
    where TKey : notnull
    where TKeyedValue : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyKeyedBucketCollection{TKey,TKeyedValue}"/> class.
    /// </summary>
    /// <param name="features">The collection of keyed values to group into buckets.</param>
    /// <param name="keyComparer">Optional key comparer for key equality checks.</param>
    protected ReadOnlyKeyedBucketCollection(IEnumerable<TKeyedValue> features, IComparer<TKey>? keyComparer = null)
    {
        KeyComparer = keyComparer ?? Comparer<TKey>.Default;
        var groups = features.OrderBy(GetKey, KeyComparer);
        Items = groups.ToArray();
        KeyCount = Items.Length;
        ItemCount = Items.Length;
    }

    /// <summary>
    /// Comparer used for key equality checks.
    /// </summary>
    public readonly IComparer<TKey> KeyComparer;
    /// <summary>
    /// Gets the key for a given keyed value.
    /// </summary>
    /// <param name="item">The keyed value.</param>
    /// <returns>The key derived from the keyed value.</returns>
    protected abstract TKey GetKey(TKeyedValue item);

    /// <summary>
    /// The array of all keyed values in this collection.
    /// </summary>
    protected readonly TKeyedValue[] Items;
    /// <summary>
    /// Alias of KeyCount for compatibility.
    /// </summary>
    public int Count => KeyCount;
    /// <summary>
    /// The number of unique keys (buckets) in this collection.
    /// </summary>
    public readonly int KeyCount;
    /// <summary>
    /// The total number of keyed values in this collection.
    /// </summary>
    public readonly int ItemCount;

    /// <summary>
    /// Checks equality between two keys.
    /// </summary>
    protected bool KeyEquals(TKey a, TKey b) => KeyComparer.Compare(a, b) == 0;

    /// <summary>
    /// All unique keys in this collection.
    /// </summary>
    /// <remarks>
    /// TODO: Optimize to avoid multiple enumerations
    /// </remarks>
    public IEnumerable<TKey> Keys => Items.Select(GetKey).Distinct(this);

    /// <summary>
    /// All value buckets in this collection.
    /// </summary>
    public IEnumerable<IEnumerable<TKeyedValue>> Values
    {
        get
        {
            var groups = Items.GroupBy(GetKey);
            return groups;
        }
    }

    /// <summary>
    /// Enumerates the keyed buckets in this collection.
    /// </summary>
    public IEnumerator<KeyValuePair<TKey, IEnumerable<TKeyedValue>>> GetEnumerator()
    {
        var groups = Items.GroupBy(GetKey);
        foreach (var group in groups)
        {
            yield return new KeyValuePair<TKey, IEnumerable<TKeyedValue>>(group.Key, group);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Checks whether a bucket with the specified key exists.
    /// </summary>
    public bool ContainsKey(TKey key) =>
        Items.Any(i => KeyEquals(GetKey(i), key));

    /// <summary>
    /// Tries to get the bucket of keyed values for the specified key.
    /// </summary>
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

    /// <summary>
    /// Gets the bucket of keyed values for the specified key.
    /// </summary>
    public IEnumerable<TKeyedValue> this[TKey key] => TryGetValue(key, out var value) ? value : Array.Empty<TKeyedValue>();

    bool IEqualityComparer<TKey>.Equals(TKey x, TKey y) => KeyComparer.Compare(x, y) == 0;

    int IEqualityComparer<TKey>.GetHashCode(TKey obj) => obj.GetHashCode();
}
