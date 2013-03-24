using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Base.GEvent
{
    using Base.GThread;
    public class Loop
    {
        public enum StatusEnum { IDLE, RUNNING, PAUSE };
        readonly object syncObject = new object();
        const int MaxThread = 10;
        public static Loop Global = new Loop();

        StatusEnum status = StatusEnum.IDLE;
        TaskPool pool;
        Queue<Response> responses = new Queue<Response>();
        List<Quest> quests = new List<Quest>();
        List<Peer> peers = new List<Peer>();

        public Loop(int threadNum = MaxThread)
        {
            pool = new TaskPool(threadNum);
        }
        ~Loop()
        {
            pool.Stop();
        }

        public bool Has(Peer peer)
        {
            return peers.Contains(peer);
        }
        public void Attach(Peer peer)
        {
            if (Has(peer))
            {
                Logger.Default.Warning("The peer is already in the loop!");
                return;
            }
            peer.BaseLoop = this;
            peers.Add(peer);
        }
        public void Detach(Peer peer)
        {
            if (Has(peer))
                peers.Remove(peer);
        }
        public void Quest(Quest quest)
        {
            quests.Add(quest);

            if (status == StatusEnum.RUNNING)
                pool.Run(new Task(quest.DoTask));
        }
        public void Response(Response response)
        {
            lock (responses)
            {
                responses.Enqueue(response);
            }
        }

        public void Start()
        {
            pool.Start();
            status = StatusEnum.RUNNING;
            foreach (Quest quest in quests)
                pool.Run(new GThread.Task(quest.DoTask));
        }
        public void Stop()
        {
            pool.Stop();
        }
        public void Join()
        {
            pool.Join();
            while (quests.Count > 0)
            {
                Logic();
                NetUtil.Sleep(1);
            }
            quests.Clear();
        }
        public void Pause()
        {
            if (status == StatusEnum.RUNNING)
            {
                status = StatusEnum.PAUSE;
                pool.Pause();
            }
        }
        public void Resume()
        {
            if (status == StatusEnum.PAUSE)
            {
                status = StatusEnum.RUNNING;
                pool.Resume();
            }
        }
        public void Logic()
        {
            lock (responses)
            {
                while (responses.Count > 0)
                {
                    Response response = responses.Dequeue();
                    response.Call();
                    if (response.Type == GEvent.Response.TypeEnum.Complete)
                    {
                        response.GetQuest.Finally();
                        quests.Remove(response.GetQuest);
                    }
                }
            }
        }
    }
}
