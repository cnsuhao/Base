using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base.Thread
{
    using System.Threading;
    class Task
    {
        public delegate void OnTaskDelegate(object arg);
        public delegate void OnEmptyDelegate();
        OnTaskDelegate onTask = null;
        OnTaskDelegate callback = null;
        object arg = null;

        public object Arg { get { return arg; } set { arg = value; } } 
        public bool IsValid { get { return onTask != null; } }

        public Task() { }
        public Task(OnTaskDelegate task) { Assign(task); }
        public Task(OnEmptyDelegate task) { Assign((_) => task()); }
        public static implicit operator Task(OnTaskDelegate task)
        {
            return new Task(task);
        }
        public static implicit operator Task(OnEmptyDelegate task)
        {
            return new Task(task);
        }

        public Task Then(OnTaskDelegate callback) { this.callback = callback; return this; }
        public void Assign(OnTaskDelegate task)
        {
            onTask = task;
        }

        public void Start(params object[] args)
        {
            if (onTask == null)
                Logger.Default.Warning("No function assign to this task!");

            if (args.Length == 0)
                onTask(arg);
            else if (args.Length == 1)
                onTask(args[0]);
            else
                onTask(args);

            if (callback != null)
                callback(this);
        }
    }
}
