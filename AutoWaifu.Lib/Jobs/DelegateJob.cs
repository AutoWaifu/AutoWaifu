using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Jobs
{
    /// <summary>
    /// An IJob that is configurable with callbacks at runtime. Essentially an IJob wrapper around delegate methods.
    /// </summary>
    public class DelegateJob : Job
    {
        public DelegateJob(ResourceConsumptionLevel resourceConsumption)
        {
            this.resourceConsumption = resourceConsumption;

            if (ResourceGroup == null)
                ResourceGroup = "Unassigned (delegate job)";
        }

        ResourceConsumptionLevel resourceConsumption;

        public string Name { get; set; }
        public string Label { get; set; }

        public Func<Task> OnStart;
        public Func<Task> OnTerminate;
        public Func<Task> OnSuspend;
        public Func<Task> OnResume;
        
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        public override ResourceConsumptionLevel ResourceConsumption => this.resourceConsumption;

        public T GetProperty<T>(string name)
        {
            if (!Properties.ContainsKey(name))
                throw new KeyNotFoundException($"No property exists with the name {name} on Job '{Name}' ('{Label}')");

            object value = Properties[name];
            Type valueType = value.GetType();

            if (!typeof(T).IsAssignableFrom(valueType))
                throw new InvalidOperationException($"Tried to read {name} property as a {typeof(T).Name} but {name} is a {valueType.Name} on job '{Name}' ('{Label}')");

            return (T)value;
        }

        protected override Task DoRun()
        {
            return OnStart?.Invoke() ?? Task.FromResult(0);
        }

        protected override Task DoTerminate()
        {
            return OnTerminate?.Invoke() ?? Task.FromResult(0);
        }

        protected override Task DoResume()
        {
            return OnResume?.Invoke() ?? base.DoResume();
        }

        protected override Task DoSuspend()
        {
            return OnSuspend?.Invoke() ?? base.DoSuspend();
        }
    }
}
