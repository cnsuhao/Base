using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Base.GEvent
{
    using Base.GThread;
    public class Quest
    {
        public delegate void OnStatusChangeDelegate(Quest quest, StatusEnum status);
        public delegate void OnStartDelegate(Quest quest);
        public delegate void OnRunningDelegate(Quest quest, float progress);
        public delegate void OnCompleteDelegate(Quest quest);
        public delegate void OnErrorDelegate(Quest quest, ErrorBundle bundle);
        public delegate void OnQuestDelegate(Quest quest);

        public enum StatusEnum { Idle, Ready, Running, Complete, Final, Error };
        Peer peer = null;
        StatusEnum status = StatusEnum.Idle;
        EventType eventType = EventType.Undefined;
        ErrorBundle erron = null;
        OnQuestDelegate task = null;

        OnStatusChangeDelegate OnStatusChange = null;
        OnStartDelegate OnStart = null;
        OnRunningDelegate OnRunning = null;
        OnCompleteDelegate OnComplete = null;
        OnErrorDelegate OnError = null;

        public string Name { private set; get; }
        public bool IsRunning { get { return status == StatusEnum.Running; } }
        public bool IsOver { get { return status == StatusEnum.Error || status == StatusEnum.Final; } }
        public StatusEnum Status { get { return status; } }
        public EventType EventType { get { return eventType; } }
        public ErrorBundle Erron { get { return erron; } }
        public Peer GetPeer { get { return peer; } }
        public Loop GetLoop { get { return peer.BaseLoop; } }

        public Quest(OnQuestDelegate task, Peer peer, EventType type = EventType.Undefined)
        {
            Assert.Null(peer, "Peer");
            this.peer = peer;
            this.eventType = type;
            this.task = task;
        }
        public Quest Start()
        {
            Assert.Null(GetLoop);
            GetLoop.Quest(this);
            return this;
        }
        public Quest Join()
        {
            while (!IsOver)
                NetUtil.Sleep(1);
            return this;
        }

        public void DoTask()
        {
            try
            {
                status = StatusEnum.Ready;
                if (OnStart != null)
                    SendResponse((quest) => OnStart(quest), Response.TypeEnum.Start);
                status = StatusEnum.Running;
                task(this);
                if (OnComplete != null)
                    SendResponse((quest) => OnComplete(quest), Response.TypeEnum.Complete);
                status = StatusEnum.Complete;
            }
            catch (Exception exception)
            {
                Error(exception.Message, ErrorCode.Break);
            }
        }
        public void Finally()
        {
            status = StatusEnum.Final;
        }

        public Quest RegisterCompleteCallback(OnCompleteDelegate callback)
        {
            this.OnComplete = callback;
            return this;
        }
        public Quest RegisterStatusChangeCallback(OnStatusChangeDelegate callback)
        {
            this.OnStatusChange = callback;
            return this;
        }
        public Quest RegisterStartCallback(OnStartDelegate callback)
        {
            this.OnStart = callback;
            return this;
        }
        public Quest RegisterErrorCallback(OnErrorDelegate callback)
        {
            this.OnError = callback;
            return this;
        }
        public Quest RegisterRunningDelegate(OnRunningDelegate callback)
        {
            this.OnRunning = callback;
            return this;
        }

        void Break()
        {
            Error("Event has been broken down!", ErrorCode.Break);
        }
        void Error(string Message, ErrorCode code = ErrorCode.Undefined)
        {
            //Log
            status = StatusEnum.Error;
            erron = new ErrorBundle(EventType, code, new BaseException(Message, "GEvent"));
            Logger.Default.ErrorNoThrow("Quest: erron since " + erron.OriginException.ToString());
            if (OnError != null)
                SendResponse((quest) => OnError(quest, erron), Response.TypeEnum.Error);
        }
        void SendResponse(Response.CallbackDelegate callback, Response.TypeEnum type = Response.TypeEnum.Undefined)
        {
            GetLoop.Response(new Response(this, callback, type));
        }
    }

    public class Response
    {
        public enum TypeEnum { Start, StatusChange, Complete, Error, Undefined };
        public delegate void CallbackDelegate(Quest quest);

        TypeEnum type = TypeEnum.Undefined;
        Quest quest;
        CallbackDelegate callback;

        public string QuestName { get { return quest.Name; } }
        public TypeEnum Type { get { return type; } }
        public Quest GetQuest { get { return quest; } }
        public Loop GetLoop { get { return quest.GetLoop; } }
        public Peer GetPeer { get { return quest.GetPeer; } }

        public Response(Quest quest, CallbackDelegate callback = null, TypeEnum type = TypeEnum.Undefined) {
            this.quest = quest;
            this.callback = callback;
            this.type = type;
        }
        public void Call()
        {
            switch (type)
            {
                case TypeEnum.Start:
                    GetPeer.OnStart(quest);
                    break;
                case TypeEnum.StatusChange:
                    GetPeer.OnStatusChange(quest);
                    break;
                case TypeEnum.Complete:
                    GetPeer.OnComplete(quest);
                    break;
                case TypeEnum.Error:
                    GetPeer.OnError(quest);
                    break;
            }
            callback(quest);
        }

        public static Response StatusChange(Quest quest, CallbackDelegate callback = null)
        {
            return new Response(quest, callback, TypeEnum.StatusChange);
        }
        public static Response Complete(Quest quest, CallbackDelegate callback = null)
        {
            return new Response(quest, callback, TypeEnum.Complete);
        }
        public static Response Error(Quest quest, CallbackDelegate callback = null)
        {
            return new Response(quest, callback, TypeEnum.Error);
        }
    }
}
