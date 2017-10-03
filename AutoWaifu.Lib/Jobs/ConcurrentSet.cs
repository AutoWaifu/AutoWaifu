using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    class ConcurrentSet<T> : IEnumerable<T>
    {
        Dictionary<T, object> data = new Dictionary<T, object>();

        public void Add(T value)
        {
            data.Add(value, null);
        }

        public void Remove(T value)
        {
            data.Remove(value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return data.Select(kvp => kvp.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
