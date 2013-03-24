/*
 *  Created by Hyf042 on 3/23/12.
 *  Copyright 2012 Hyf042. All rights reserved.
 * 
 *  Logger.cs
 */
#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1
#define UNITY
#else
#define WIN
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Base
{
    public interface ILoggerAdatper
    {
        void Log(string message, Logger.Level level);
    }

    public interface ILoggerDecorator
    {
        string Header();
        string Decorate(Logger.Bundle bundle);
    }

    public class Logger
    {
        public class NormalDecorator : ILoggerDecorator
        {
            public string Header() {
                return "==================================================\n"
                        + "Logger Start\n" +
                       "=================================================="; 
            }
            public string Decorate(Logger.Bundle bundle)
            {
                Logger logger = bundle.Log;
                string realLog = "";

                if (logger.HasFeature(Feature.PrependRunningTime))
                    realLog += String.Format("[{0}] ", bundle.Time.ToString());

                if (logger.HasFeature(Feature.PrependLevelName))
                    realLog += "#"+bundle.Level.ToString()+"# ";

                realLog += bundle.Message;

                return realLog;
            }
        }
        public class HTMLDecorator : ILoggerDecorator
        {
            public string Header()
            {
                return "<!--"
                        + "Logger Start" +
                       "-->";
            }
            public string Decorate(Logger.Bundle bundle)
            {
                Logger logger = bundle.Log;
                string realLog = "";

                realLog += "<p>";
                if (logger.HasFeature(Feature.PrependRunningTime))
                    realLog += String.Format("[{0}] ", bundle.Time.ToString());

                if (logger.HasFeature(Feature.PrependLevelName))
                    realLog += "#" + bundle.Level.ToString() + "# ";

                realLog += bundle.Message;
                realLog += "</p>";

                return realLog;
            }
        }

        public class NullAdapter : ILoggerAdatper
        {
            public void Log(string message, Level level) {}
        }
        public class ConsoleAdapter : ILoggerAdatper
        {
            public void Log(string message, Level level)
            {
#if WIN
                ConsoleColor color = Console.ForegroundColor;

                switch (level)
                {
                    case Level.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        break;

                    case Level.Warn:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;

                    case Level.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                }
                Console.WriteLine(message);
                Console.ForegroundColor = color;
#elif UNITY
                UnityEngine.Debug.Log(message);
#endif
            }
        }
        public class StreamAdapter : ILoggerAdatper
        {
            StreamWriter writer;

            public StreamAdapter (Stream stream)
            {
                writer = new StreamWriter(stream);
            }

            ~StreamAdapter()
            {
                writer.Close();
            }

            public void Log(string message, Level level)
            {
                writer.WriteLine(message);
                writer.Flush();
            }
        }

        public enum Feature
        {
            Frugal = 0,
            PrependRunningTime = 1,
            PrependLevelName = 2,
            OutputToConsole = 4,
            ThrowError = 8,
            Standard = PrependRunningTime | PrependLevelName | OutputToConsole | ThrowError
        }

        public enum Level : byte
        {
            Info = 0,
            Debug = 1,
            Warn = 2,
            Error = 4,
            All = 7
        }

        public class Bundle
        {
            public Logger Log;
            public string Message;
            public Level Level;
            public TimeSpan Time;
            public Bundle(Logger log, string message, Level level, TimeSpan time)
            {
                this.Log = log;
                this.Message = message;
                this.Level = level;
                this.Time = time;
            }
        }

        Feature feature = Feature.Standard;
        ILoggerDecorator decorator = null;
        ILoggerAdatper adapter = null;
        const string defaultLogFile = "base.log";
        List<Bundle> logList = new List<Bundle>();
        readonly object syncRoot = new object();

        // Static Attribute
        static ILoggerAdatper consoleAdapter = new ConsoleAdapter();
        static Stopwatch timer = new Stopwatch();
        public static Logger Default = new Logger(defaultLogFile);

        // Log List About
        public List<Bundle> LogList { get { return logList; } }
        public string[] LogDump() { 
            List<string> dump = new List<string>();
            foreach (Bundle log in logList)
                dump.Add(decorator.Decorate(log));
            return dump.ToArray();
        }
        public void ClearLogList() { logList.Clear(); }

        public Logger(ILoggerDecorator decorator = null,
                      ILoggerAdatper adapter = null)
        {
            Assign(decorator, adapter);
        }
        public Logger(Stream stream, ILoggerDecorator decorator = null)
        {
            Assign(decorator, new StreamAdapter(stream));
        }
        public Logger(string filename, ILoggerDecorator decorator = null)
            :this(new FileStream(filename, FileMode.Create), decorator)
        {
        }
        ~Logger()
        {
        }

        private void Assign(ILoggerDecorator decorator = null,
                      ILoggerAdatper adapter = null)
        {
            if (!timer.IsRunning)
                timer.Start();

            if (decorator == null)
                this.decorator = new NormalDecorator();
            else
                this.decorator = decorator;

            this.adapter = adapter;

            LoggerBegin();
        }
        private void LoggerBegin()
        {
            RawLog(decorator.Header());
        }
        private void RawLog(string log, Level level = Level.Info)
        {
#if !DEBUG
            if (level == Level.Debug)
                return;
#endif
            if (log == "")
                return;

            lock (syncRoot)
            {
                if (adapter != null)
                    adapter.Log(log, level);

                if (HasFeature(Feature.OutputToConsole) && consoleAdapter != null)
                    consoleAdapter.Log(log, level);
            }
        }

        public void SetFeature(Feature feature, bool flag)
        {
            if (flag)
                this.feature |= feature;
            else
                this.feature &= ~feature;
        }
        public bool HasFeature(Feature feature)
        {
            return (this.feature & feature) == feature;
        }
        public void RedirectStream(Stream stream)
        {
            adapter = new StreamAdapter(stream);
        }

        public void Log(string log, Level level = Level.Info)
        {
            log = log ?? "<Null>";

            lock (syncRoot)
            {
                Bundle bundle = new Bundle(
                    this,
                    log,
                    level,
                    timer.Elapsed
                );

                log = decorator.Decorate(bundle);

                RawLog(log, level);

                if (level == Level.Error && HasFeature(Feature.ThrowError))
                    throw new BaseException(log, "Logger");

                logList.Add(bundle);
            }
        }
        public void Log(string log, Level level, params object[] args)
        {
            if (args.Length > 0)
                Log(String.Format(log, args), level);
            else
                Log(log, level);
        }

        public void Info(string log, params object[] args)
        {
            Log(log, Level.Info, args);
        }
        public void Error(string log, params object[] args)
        {
            Log(log, Level.Error, args);
        }
        public void ErrorNoThrow(string log, params object[] args)
        {
            bool flag = HasFeature(Feature.ThrowError);
            SetFeature(Feature.ThrowError, false);
            Error(log, args);
            SetFeature(Feature.ThrowError, true);
        }
        public void Warning(string log, params object[] args)
        {
            Log(log, Level.Warn, args);
        }
        public void Debug(string log, params object[] args)
        {
            Log(log, Level.Debug, args);
        }
    }
}
