using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace TumblThree.Domain
{
    public static class Logger
    {
        private static TraceSource _traceSource;

        public static void Initialize(string logPath, TraceLevel maximumLogLevel)
        {
            string filename = Path.Combine(logPath, "TumblThree.log");
            TextWriterTraceListener listener = (TextWriterTraceListener)Trace.Listeners["MyListener"];
            if (listener != null) Trace.Listeners.Remove(listener);
            listener = new TextWriterTraceListener(filename, "MyListener");
            SourceLevels srcLevel;
            switch (maximumLogLevel)
            {
                case TraceLevel.Verbose:
                    srcLevel = SourceLevels.Verbose;
                    break;
                case TraceLevel.Info:
                    srcLevel = SourceLevels.Information;
                    break;
                case TraceLevel.Warning:
                    srcLevel = SourceLevels.Warning;
                    break;
                case TraceLevel.Error:
                    srcLevel = SourceLevels.Error;
                    break;
                default:
                    srcLevel = SourceLevels.Off;
                    break;
            }
            _traceSource = new TraceSource("TumblThreeApp.TumblThree");
            _traceSource.Switch = new SourceSwitch("MySourceSwitch", srcLevel.ToString());
            _traceSource.Listeners.Add(listener);
#if DEBUG
            Trace.AutoFlush = true;
#else
            Trace.AutoFlush = false;
#endif
            Trace.IndentSize = 4;
        }

        [Conditional("DEBUG")]
        public static void Verbose(string format, params object[] arguments)
        {
            Log(TraceEventType.Verbose, format, arguments);
        }

        public static void Information(string format, params object[] arguments)
        {
            Log(TraceEventType.Information, format, arguments);
        }

        public static void Warning(string format, params object[] arguments)
        {
            Log(TraceEventType.Warning, format, arguments);
        }

        public static void Error(string format, params object[] arguments)
        {
            Log(TraceEventType.Error, format, arguments);
        }

        private static void Log(TraceEventType eventType, string format, params object[] arguments)
        {
            if (_traceSource.Switch.ShouldTrace(eventType))
            {
                string tracemessage = $"{DateTime.Now:yyyyMMdd HH:mm:ss.fff}\t{Shorten(eventType),-4}\t{string.Format(CultureInfo.InvariantCulture, format, arguments)}";
                foreach (TraceListener listener in _traceSource.Listeners)
                {
                    listener.WriteLine(tracemessage);
                    if (Trace.AutoFlush) listener.Flush();
                }
            }
        }

        private static string Shorten(TraceEventType eventType)
        {
            return Regex.Replace(eventType.ToString(), "[aeiou]", "", RegexOptions.CultureInvariant).Substring(0, 3);
        }

        public static string GetMemberName([CallerMemberName] string memberName = null)
        {
            return memberName;
        }
    }
}
