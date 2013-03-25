using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Base.GEvent
{
    using Base.GThread;
    /// <summary>
    /// QuestBundle, contains the arguments of quest & return value of quest
    /// The result object is the result of quest, it will gurantee to be assigned after quest complete
    /// The quest value is alternativly to be assign if there is any need
    /// </summary>
    public class QuestBundle
    {
        Quest quest;
        Hashtable questParams = null;
        Hashtable questValue = null;
        object result = null;
        readonly object syncRoot = new object();

        public Quest _Quest
        {
            get
            {
                Logger.Default.Warning("You should be careful of getting quest from bundle!");
                return quest;
            }
        }
        public object Result { 
            get {
                if (quest.IsRunning)
                    Logger.Default.Error("GEvent.QuestBundle: You can not request the result of a running bundle!");
                lock (syncRoot)
                {
                    return result;
                }
            }
            set
            {
                lock (syncRoot)
                {
                    result = value;
                }
            }
        }
        public QuestBundle(Quest quest, Hashtable args)
        {
            this.quest = quest;
            this.questParams = args;
        }

        public void Params(Hashtable args)
        {
            questParams = args.Clone() as Hashtable;
        }
        public void SetValue(string key, object value)
        {
            if (questValue == null)
                questValue = new Hashtable();

            lock (questValue)
            {
                questValue[key] = value;
            }
        }
        public object GetValue(string key)
        {
            if (questValue == null)
                Logger.Default.Error("GEvent.QuestBundle: You can not Get a null value!");

            lock (questValue)
            {
                return questValue[key];
            }
        }

        public object this[string key]
        {
            get
            {
                return questParams[key];
            }
        }
    }

    public class Quest
    {
        public delegate void OnStatusChangeDelegate(Quest quest, StatusEnum status);
        public delegate void OnStartDelegate(Quest quest);
        public delegate void OnRunningDelegate(Quest quest, float progress);
        public delegate void OnCompleteDelegate(Quest quest, object resule);
        public delegate void OnErrorDelegate(Quest quest, ErrorBundle bundle);
        public delegate object OnQuestDelegate(QuestBundle bundle);

        public enum StatusEnum { Idle, Ready, Running, Complete, Final, Error };
        Peer peer = null;
        StatusEnum status = StatusEnum.Idle;
        EventType eventType = EventType.Undefined;
        ErrorBundle erron = null;
        OnQuestDelegate task = null;
        QuestBundle questBundle;

        OnStatusChangeDelegate OnStatusChange = null;
        OnStartDelegate OnStart = null;
        OnRunningDelegate OnRunning = null;
        OnCompleteDelegate OnComplete = null;
        OnErrorDelegate OnError = null;

        public string Name { private set; get; }
        public bool IsRunning { get { return status == StatusEnum.Running; } }
        public bool IsOver { get { return status == StatusEnum.Error || status == StatusEnum.Final; } }
        public bool IsStop { get { return IsOver || status == StatusEnum.Complete; } }
        public StatusEnum Status { get { return status; } }
        public EventType EventType { get { return eventType; } }
        public ErrorBundle Erron { get { return erron; } }
        public QuestBundle Bundle { get { return questBundle; } }
        public Peer GetPeer { get { return peer; } }
        public Loop GetLoop { get { return peer.BaseLoop; } }

        public Quest(OnQuestDelegate task, Peer peer, EventType type = EventType.Undefined, Hashtable args = null)
        {
            Assert.Null(peer, "Peer");
            this.peer = peer;
            this.eventType = type;
            this.task = task;
            questBundle = new QuestBundle(this, args);
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
        public Quest Params(Hashtable args)
        {
            if (IsRunning)
                Logger.Default.Error("GEvent.Quest: You can not reset param on a running task!");
            lock (questBundle)
            {
                questBundle.Params(args);
            }
            return this;
        }
        public Quest Params(params object[] args)
        {
            Params(Utility.HashTable(args));
            return this;
        }

        public void DoTask()
        {
            try
            {
                status = StatusEnum.Ready;
                if (OnStart != null)
                    SendResponse((quest) => OnStart(quest), Response.TypeEnum.Start);
                else
                    NullResponse(Response.TypeEnum.Start);
                status = StatusEnum.Running;
                lock (questBundle)
                {
                    questBundle.Result = task(questBundle);
                }
                if (OnComplete != null)
                    SendResponse((quest) => OnComplete(quest, questBundle.Result), Response.TypeEnum.Complete);
                else
                    NullResponse(Response.TypeEnum.Complete);
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
            else
                NullResponse(Response.TypeEnum.Error);
        }
        void SendResponse(Response.CallbackDelegate callback, Response.TypeEnum type = Response.TypeEnum.Undefined)
        {
            GetLoop.Response(new Response(this, callback, type));
        }
        void NullResponse(Response.TypeEnum type = Response.TypeEnum.Undefined)
        {
            GetLoop.Response(new Response(this, null, type));
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
            if (callback != null)
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
