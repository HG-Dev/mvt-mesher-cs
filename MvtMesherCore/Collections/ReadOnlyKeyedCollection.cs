using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MvtMesherCore.Collections;

public abstract class ReadOnlyKeyedCollection<TKey, TKeyedValue>(IEnumerable<TKeyedValue> features) : IReadOnlyDictionary<TKey, TKeyedValue>
    where TKey : notnull
    where TKeyedValue : notnull // A null value cannot possess a key.
{
    protected virtual EqualityComparer<TKey> KeyComparer => EqualityComparer<TKey>.Default;
    protected abstract TKey GetKey(TKeyedValue item);
    
    protected readonly TKeyedValue[] Items = features.ToArray();
    protected (TKey key, int idx)? LastUsed = null;
    
    public IEnumerator<KeyValuePair<TKey, TKeyedValue>> GetEnumerator()
    {
        foreach (var feature in Items)
            yield return new KeyValuePair<TKey, TKeyedValue>(GetKey(feature), feature);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => Items.Length;
    public bool ContainsKey(TKey key)
    {
        if (LastUsed.HasValue 
            && KeyComparer.Equals(LastUsed.Value.key, key))
            return true;
        foreach (var item in Items)
            if (KeyComparer.Equals(GetKey(item), key))
                return true;
        return false;
    }

    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TKeyedValue value)
    {
        if (LastUsed.HasValue 
            && KeyComparer.Equals(LastUsed.Value.key, key))
        {
            value = Items[LastUsed.Value.idx];
            return true;
        }

        for (int i = 0; i < Items.Length; i++)
        {
            var item = Items[i];
            if (KeyComparer.Equals(GetKey(item), key))
            {
                LastUsed = (key, i);
                value = item;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public TKeyedValue this[TKey key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();

    public IEnumerable<TKey> Keys => Items.Select(GetKey);
    public IEnumerable<TKeyedValue> Values => Items;
}