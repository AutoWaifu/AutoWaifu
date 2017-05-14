using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib
{
    public class ConcurrentHashSet<T> : IEnumerable<T>, IEnumerable where T : class
    {
        ConcurrentDictionary<T, T> dataStore = new ConcurrentDictionary<T, T>();

        public int Count => dataStore.Count;

        public bool Contains(T value)
        {
            return dataStore.ContainsKey(value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var value in dataStore)
                yield return value.Value;
        }

        public bool TryGet(out T value)
        {
            var data = dataStore.Keys.FirstOrDefault();
            if (data != null)
            {
                value = data;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryTake(out T value)
        {

                T junk;

                var data = dataStore.Keys.FirstOrDefault();
                if (data != null)
                    dataStore.TryRemove(data, out junk);

                value = data;
                return value != null;

        }

        public bool TryAdd(T value)
        {
            return dataStore.TryAdd(value, value);
        }

        public bool TryRemove(T value)
        {
            T junk;
            return dataStore.TryRemove(value, out junk);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var value in dataStore)
                yield return value.Value;
        }
    }
}
