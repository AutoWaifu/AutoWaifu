using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AutoWaifu2
{
    public class ThreadObservableCollection<T> : ObservableCollection<T>
    {
        public ThreadObservableCollection(Dispatcher dispatcher)
        {
            this.Dispatcher = dispatcher;
        }

        public Dispatcher Dispatcher { get; }

        public new void Add(T item)
        {
            Dispatcher.Invoke(() => base.Add(item));
        }

        public new void Remove(T item)
        {
            Dispatcher.Invoke(() => base.Remove(item));
        }
    }
}
