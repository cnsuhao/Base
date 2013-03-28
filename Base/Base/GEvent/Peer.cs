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
        public bool IsEmpty { get{return waitings.Count == 0;} }
        public bool IsBusy { get { return !IsEmpty; } }

        public Peer(Loop loop = null)
        {
            if (loop == null)
                loop = Loop.Global;
            loop.Attach(this);
        }

        public Quest Take()
        {
            if (IsEmpty)
            {
                Logger.Default.Warning("GEvent.Quest: You can not take a empty peer!");
                return null;
            }
            return waitings.Dequeue();
        }
        public Quest Peek()
        {
            if (IsEmpty)
            {
                Logger.Default.Warning("GEvent.Quest: You can not take a empty peer!");
                return null;
            }
            return waitings.Peek();
        }
        public void Next()
        {
            if (IsEmpty)
            {
                Logger.Default.Warning("GEvent.Quest: This quest should not be empty!");
                return;
            }
            Take();
            if (IsEmpty)
                return;
            BaseLoop.Quest(Peek());
        }
        public void _Run(Quest quest)
        {
            if (IsEmpty)
                BaseLoop.Quest(quest);
        
            waitings.Enqueue(quest);
        }
        public void Invoke(PeerTaskDelegate task)
        {
            BaseLoop.Invoke(() => task(this));
        }
        public void Invoke(Loop.EmptyTaskDelegate task)
        {
            BaseLoop.Invoke(task);
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
