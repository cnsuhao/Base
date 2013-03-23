using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Base.GEvent
{
    #region Event
    public enum EventType
    {
        WEB,

        Undefined
    }
    #endregion

    #region Errors
    public enum ErrorCode
    {
        NoError,

        WebError,

        Break,

        Undefined
    };

    public class ErrorBundle
    {
        EventType eventType = EventType.Undefined;
        ErrorCode erron  = ErrorCode.NoError;
        string description = "";
        System.Exception exception = null;

        public EventType Event { get { return eventType; } }
        public ErrorCode Erron { get { return erron; } }
        public string Description { get { return description; } }
        public System.Exception OriginException { get { return exception; } }

        public ErrorBundle(EventType type, ErrorCode erron = ErrorCode.NoError, string description = "", System.Exception exception = null)
        {
            this.eventType = type;
            this.erron = erron;
            this.description = description;
            this.exception = exception;
        }
        public ErrorBundle(EventType type, ErrorCode erron, System.Exception exception)
        {
            this.eventType = type;
            this.erron = erron;
            this.description = exception.Message;
            this.exception = exception;
        }
    };
    public class GEventException : BaseException
    {
        public GEventException(ErrorBundle erron)
            : base(erron.Description, "GEvent")
        {
        }
    }
    #endregion

    #region Util
    class NetUtil
    {
        public static bool Ping(string url)
        {
            return true;
        }

        public static bool isConnect
        {
            get { return Ping("http://www.baidu.com"); }
        }
    };
    #endregion
}
