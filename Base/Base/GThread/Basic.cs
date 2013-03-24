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

        public void Wait()
        {
            Monitor.Wait(sync);
        }

        public void Notify()
        {
            Monitor.Pulse(sync);
        }

        public void NotifyAll()
        {
            Monitor.PulseAll(sync);
        }
    }
}
