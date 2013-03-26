using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base.GEvent
{
    public class Peer
    {
        public delegate void TaskDelegate(Peer peer);
        public delegate void EmptyTaskDelegate();
        
        Loop loop = null;

        public Loop BaseLoop { set { loop = value; } get { return loop; } }

        public Peer(Loop loop = null)
        {
            if (loop == null)
                loop = Loop.Global;
            loop.Attach(this);
        }

        public void Invoke(TaskDelegate task)
        {
            BaseLoop.Response(new Response(null, (_) => task(this), Response.TypeEnum.External));
        }
        public void Invoke(EmptyTaskDelegate task)
        {
            BaseLoop.Response(new Response(null, (_) => task(), Response.TypeEnum.External));
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
