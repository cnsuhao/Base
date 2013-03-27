using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.GEvent
{
    public class CommonQuest : Quest
    {
        public string Content { get; set; }
        public CommonQuest(OnQuestDelegate task, Peer peer)
            : base(task, peer, EventType.Common)
        {
        }
    }

    class UtilPeer : Peer
    {
        public Quest After(Peer.PeerTaskDelegate func, int milisecond = 0, bool async = false)
        {
            return After(() => func(this), milisecond, async);
        }
        public Quest After(Loop.EmptyTaskDelegate func, int milisecond = 0, bool async = false)
        {
            return new CommonQuest(
                (bundle) =>
                {
                    System.Threading.Thread.Sleep(milisecond);
                    if (async)
                        func();
                    else
                        Invoke(func);
                    return null;
                },
                this
            );
        }
    }
}
