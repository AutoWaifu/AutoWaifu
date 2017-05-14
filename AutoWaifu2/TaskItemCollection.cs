using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    public class TaskItemCollection : List<TaskItem>
    {
        public TaskItemCollection()
        {
            orderedItems.Add(TaskItemState.Processing, new List<TaskItem>());
            orderedItems.Add(TaskItemState.Pending, new List<TaskItem>());
            orderedItems.Add(TaskItemState.Done, new List<TaskItem>());

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





        Dictionary<TaskItemState, List<TaskItem>> orderedItems = new Dictionary<TaskItemState, List<TaskItem>>();

        public TaskItem[] this[TaskItemState state]
        {
            get
            {
                return orderedItems[state].ToArray();
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

        public new void AddRange(IEnumerable<TaskItem> items)
        {
            foreach (var item in items)
            {
                if (item.RelativeFilePath == null || item.State == TaskItemState.Unknown)
                    throw new InvalidOperationException();
            }

            foreach (var item in items)
            {
                item.StateChanged += Item_StateChanged;
                InvokeAddStateEvent(item, item.State);
            }

            base.AddRange(items);
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
            InvokeRemoveStateEvent(item, oldState);
            InvokeAddStateEvent(item, newState);
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
    }
}
