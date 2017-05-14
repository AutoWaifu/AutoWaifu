using AutoWaifu.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib
{
    public class ObservableConcurrentHashSet<T> : ConcurrentHashSet<T> where T : class
    {
        public new bool TryAdd(T value)
        {
            bool success = base.TryAdd(value);
            if (success)
                ItemAdded?.Invoke(value);
            return success;
        }

        public new bool TryRemove(T value)
        {
            bool success = base.TryRemove(value);
            if (success)
                ItemRemoved?.Invoke(value);
            return success;
        }


        public event Action<T> ItemAdded;
        public event Action<T> ItemRemoved;
    }
}
