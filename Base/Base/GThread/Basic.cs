using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Base.GThread
{
    class Condition
    {
        delegate void MonitorDelegate();

        object sync;

        public Condition(object sync)
        {
            this.sync = sync;
        }

        void Call(MonitorDelegate call)
        {
            bool flag = Monitor.IsEntered(sync);
            if (flag)
                call();
            else
            {
                Monitor.Enter(sync);
                try
                {
                    call();
                }
                finally
                {
                    Monitor.Exit(sync);
                }
            }
        }

        public void Wait()
        {
            Call(() => Monitor.Wait(sync));
        }

        public void Notify()
        {
            Call(() => Monitor.Pulse(sync));
        }

        public void NotifyAll()
        {
            Call(() => Monitor.PulseAll(sync));
        }
    }
}
