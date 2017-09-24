using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    public class TaskItemCollection : ObservableCollection<TaskItem>
    {
        public TaskItemCollection()
        {
            orderedItems.TryAdd(TaskItemState.Processing, new List<TaskItem>());
            orderedItems.TryAdd(TaskItemState.Pending, new List<TaskItem>());
            orderedItems.TryAdd(TaskItemState.Done, new List<TaskItem>());

            this.AddedInputItem += (item) => this.orderedItems[TaskItemState.Pending].Add(item);
            this.AddedItemProcessing += (item) => this.orderedItems[TaskItemState.Processing].Add(item);
            this.AddedOutputItem += (item) => this.orderedItems[TaskItemState.Done].Add(item);

            this.RemovedInputItem += (item) => this.orderedItems[TaskItemState.Pending].Remove(item);
            this.RemovedItemProcessing += (item) => this.orderedItems[TaskItemState.Processing].Remove(item);
            this.RemovedOutputItem += (item) => this.orderedItems[TaskItemState.Done].Remove(item);
        }



        /// <param name="filePath">Relative path or input/output path for a task file</param>
        public TaskItem TaskFor(string filePath)
        {
            return this.SingleOrDefault(i => i.RelativeFilePath == filePath || i.InputPath == filePath || i.OutputPath == filePath);
        }

        public TaskItem this[string path]
        {
            get { return TaskFor(path); }
        }





        ConcurrentDictionary<TaskItemState, List<TaskItem>> orderedItems = new ConcurrentDictionary<TaskItemState, List<TaskItem>>();

        public TaskItem[] this[TaskItemState state]
        {
            get
            {
                var items = orderedItems[state].ToArray();
                if (items == null)
                    items = new TaskItem[0];

                return items;
            }
        }



        void InvokeAddStateEvent(TaskItem item, TaskItemState state)
        {
            switch (state)
            {
                case TaskItemState.Pending:
                    AddedInputItem?.Invoke(item);
                    break;
                case TaskItemState.Processing:
                    AddedItemProcessing?.Invoke(item);
                    break;
                case TaskItemState.Done:
                    AddedOutputItem?.Invoke(item);
                    break;
            }
        }

        void InvokeRemoveStateEvent(TaskItem item, TaskItemState state)
        {
            switch (state)
            {
                case TaskItemState.Pending:
                    RemovedInputItem?.Invoke(item);
                    break;
                case TaskItemState.Processing:
                    RemovedItemProcessing?.Invoke(item);
                    break;
                case TaskItemState.Done:
                    RemovedOutputItem?.Invoke(item);
                    break;
            }
        }


        public new void Add(TaskItem item)
        {
            if (item.RelativeFilePath == null || item.State == TaskItemState.Unknown)
                throw new InvalidOperationException();

            item.StateChanged += Item_StateChanged;
            InvokeAddStateEvent(item, item.State);

            base.Add(item);
        }

        public void AddRange(IEnumerable<TaskItem> items)
        {
            foreach (var item in items)
            {
                if (item.RelativeFilePath == null || item.State == TaskItemState.Unknown)
                    throw new InvalidOperationException();

                item.StateChanged += Item_StateChanged;
                InvokeAddStateEvent(item, item.State);

                this.Add(item);
            }
        }

        public void Remove(string filePath)
        {
            var item = this[filePath];
            if (item != null)
            {
                InvokeRemoveStateEvent(item, item.State);

                item.StateChanged -= Item_StateChanged;
                this.Remove(item);
            }
        }





        private void Item_StateChanged(TaskItem item, TaskItemState oldState, TaskItemState newState)
        {
            if (oldState != newState)
            {
                InvokeRemoveStateEvent(item, oldState);
                InvokeAddStateEvent(item, newState);
            }

            TaskItemChanged?.Invoke(item);
        }



        public IEnumerable<TaskItem> InputItems { get; }
        public IEnumerable<TaskItem> ProcessingItems { get; }
        public IEnumerable<TaskItem> OutputItems { get; }



        public event Action<TaskItem> AddedInputItem;
        public event Action<TaskItem> AddedItemProcessing;
        public event Action<TaskItem> AddedOutputItem;

        public event Action<TaskItem> RemovedInputItem;
        public event Action<TaskItem> RemovedItemProcessing;
        public event Action<TaskItem> RemovedOutputItem;

        public event Action<TaskItem> TaskItemChanged;
    }
}
