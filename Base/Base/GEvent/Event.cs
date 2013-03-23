using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Base.GEvent
{
    public abstract class Event
    {
        public delegate void OnStatusChangeDelegate (Event source, StatusEnum status);
        public delegate bool OnStartDelegate (Event source);
        public delegate bool OnRunningDelegate (Event source, float progress);
        public delegate void OnCompleteDelegate (Event source);
        public delegate void OnErrorDelegate (Event source, ErrorBundle bundle);
        public enum StatusEnum {Idle, Ready, Running, Complete, Error};

        StatusEnum status = StatusEnum.Idle;
        Peer peer = null;
        Loop loop = null;
        EventType eventType = EventType.Undefined;
        ErrorBundle erron = null;

        public OnStatusChangeDelegate OnStatusChange = null;
        public OnStartDelegate OnStart = null;
        public OnRunningDelegate OnRunning = null;
        public OnCompleteDelegate OnComplete = null;
        public OnErrorDelegate OnError = null;

        public StatusEnum Status { get { return status; } }
        public EventType Type { get { return eventType; } }
        public ErrorBundle Error { get { return erron; } }
        public bool IsError { get { return status == StatusEnum.Error; } }
        public Peer GetPeer() {return peer;}
        public Loop GetLoop() {return loop;}

        public Event(Peer peer, Loop loop, EventType type = EventType.Undefined)
        {
            this.peer = peer;
            this.loop = loop;
            this.eventType = type;
        }

        public abstract void DoTask();

        public bool Start()
        {
            status = StatusEnum.Ready;
            if (OnStart != null)
                if (!OnStart(this))
                {
                    BreakDown();
                    return false;
                }

            status = StatusEnum.Running;

            DoTask();

            return true;
        }

        void BreakDown()
        {
            ErrorOccur("Event has been broken down!", ErrorCode.Break);
        }

        void ErrorOccur(string Message, ErrorCode code = ErrorCode.Undefined)
        {
            //Log
            status = StatusEnum.Error;
            erron = new ErrorBundle(Type, code, new BaseException(Message, "GEvent"));
            if (OnError != null)
                OnError(this, Error);
        }
    }
}
