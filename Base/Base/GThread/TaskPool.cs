using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Base.GThread
{
    using System.Threading;
    class TaskPool
    {
        public const int DefaultMaxThread = 10;
        List<Thread> threads = new List<Thread>();
        Queue<Task> tasks = new Queue<Task>();
        Condition condition;
        bool isRunning;
        bool isAccept;

        public bool IsRunning { get { return isRunning; } }
        public bool IsAccept { get { return isAccept; } }
        public int ThreadCount { get{return threads.Count;} }
        public int TaskCount { get { return tasks.Count; } }

        public TaskPool(int threadNum = DefaultMaxThread)
        {
            isRunning = false;
            isAccept = false;
            condition = new Condition(tasks);

            for (int i = 0; i < threadNum; i++)
                threads.Add(new Thread(RunInThread));
        }
        public void Start()
        {
            Join();

            isRunning = true;
            isAccept = true;
            foreach (Thread thread in threads)
                thread.Start();
        }
        public void Join()
        {
            if (IsRunning)
            {
                isAccept = false;

                while (TaskCount > 0)
                    Thread.Sleep(1);

                Stop();
            }
        }
        public void Stop()
        {
            if (IsRunning)
            {
                isRunning = false;
                condition.NotifyAll();

                foreach (Thread thread in threads)
                    thread.Join();
            }
        }
        public void Append()
        {
            Thread newThread = new Thread(RunInThread);
            threads.Add(newThread);
            if (IsRunning)
                newThread.Start();
        }
        public void Pause()
        {
            isAccept = false;
        }
        public void Resume()
        {
            isAccept = true;
        }
        public void Resize(int threadNum)
        {
            if (IsRunning)
            {
                Logger.Default.Warning("TaskPool: You can not resize pool during running!");
                return;
            }

            threads.Clear();
            for (int i = 0; i < threadNum; i++)
                threads.Add(new Thread(RunInThread));
        }
        public void Run(Task task)
        {
            if (!IsRunning)
            {
                Logger.Default.Warning("TaskPool: This pool does not start yet!");
                return;
            }
            if (!IsAccept)
            {
                Logger.Default.Warning("TaskPool: This pool does not accept task!");
                return;
            }
            if (ThreadCount == 0)
                task.Start();
            else
            {
                lock (tasks)
                {
                    tasks.Enqueue(task);
                    condition.Notify();
                }
            }
        }
        public void Run(Task[] tasks)
        {
            if (!IsRunning)
            {
                Logger.Default.Warning("TaskPool: This pool does not start yet!");
                return;
            }
            if (!IsAccept)
            {
                Logger.Default.Warning("TaskPool: This pool does not accept task!");
                return;
            }
            if (ThreadCount == 0)
                foreach (Task task in tasks)
                    task.Start();
            else
            {
                lock (tasks)
                {
                    foreach (Task task in tasks)
                        this.tasks.Enqueue(task);
                    condition.NotifyAll();
                }
            }
        }

        Task Take()
        {
            lock (tasks)
            {
                while (isRunning && tasks.Count == 0)
                    condition.Wait();

                if (tasks.Count == 0)
                    return null;

                return tasks.Dequeue();
            }
        }
        void RunInThread()
        {
            try
            {
                while (IsRunning)
                {
                    Task task = Take();
                    if (task == null)
                        continue;
                    task.Start();
                }
            }
            catch(Exception exception)
            {
                Logger.Default.ErrorNoThrow("TaskPool: error executing task, " + exception.Message);
            }
        }
    }
}
