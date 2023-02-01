public class LimitedSizeDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    private int _limit;
    private Queue<TKey> _keyQueue;

    public LimitedSizeDictionary(int limit)
    {
        _limit = limit;
        _keyQueue = new Queue<TKey>();
    }

    public new TValue this[TKey key]
    {
        get
        {
            TValue value = base[key];
            _keyQueue.Enqueue(key);
            return value;
        }
        set
        {
            if (base.ContainsKey(key))
            {
                _keyQueue.Enqueue(key);
                base[key] = value;
            }
            else
            {
                base.Add(key, value);
                _keyQueue.Enqueue(key);
                if (Count > _limit)
                {
                    TKey removedKey = _keyQueue.Dequeue();
                    base.Remove(removedKey);
                }
            }
        }
    }

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        _keyQueue.Enqueue(key);
        if (Count > _limit)
        {
            TKey removedKey = _keyQueue.Dequeue();
            base.Remove(removedKey);
        }
    }

    public bool ContainsKey(TKey key)
    {
        return base.ContainsKey(key);
    }
}
