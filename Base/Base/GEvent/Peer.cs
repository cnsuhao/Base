using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base.GEvent
{
    public class Peer
    {
        Loop loop = null;

        public Loop BaseLoop { set { loop = value; } get { return loop; } }

        public Peer(Loop loop = null)
        {
            if (loop == null)
                loop = Loop.Global;
            loop.Attach(this);
        }

        public virtual void OnStatusChange(Quest quest)
        {
        }
        public virtual void OnComplete(Quest quest)
        {
        }
        public virtual void OnError(Quest quest)
        {
        }
        public virtual void OnStart(Quest quest) { }
    }
}
