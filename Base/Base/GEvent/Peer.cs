using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base.GEvent
{
    public class Peer
    {
        Queue<Quest> waitings = new Queue<Quest>();

        public delegate void PeerTaskDelegate(Peer peer);

        Loop loop = null;

        public Loop BaseLoop { set { loop = value; } get { return loop; } }

        public Peer(Loop loop = null)
        {
            if (loop == null)
                loop = Loop.Global;
            loop.Attach(this);
        }

        #region Quest Logic
        // Run Quest Logic, the default implement is a blocking queue
        protected void BlockingNext()
        {
            if (waitings.Count == 0)
            {
                Logger.Default.Warning("GEvent.Quest: This quest should not be empty!");
                return;
            }
            waitings.Dequeue();
            if (waitings.Count == 0)
                return;
            BaseLoop.Quest(waitings.Peek());
        }
        protected void BlockingRun(Quest quest)
        {
            if (waitings.Count == 0)
                BaseLoop.Quest(quest);

            waitings.Enqueue(quest);
        }
        public virtual void Next()
        {
            BlockingNext();
        }
        public virtual void _Run(Quest quest)
        {
            BlockingRun(quest);
        }
        #endregion

        #region Util Funcs
        public void Invoke(PeerTaskDelegate task)
        {
            BaseLoop.Invoke(() => task(this));
        }
        public void Invoke(Loop.EmptyTaskDelegate task)
        {
            BaseLoop.Invoke(task);
        }
        #endregion

        #region Callback Funcs
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
        #endregion
    }
}
