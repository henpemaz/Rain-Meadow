using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public class CustomLogListener : ILogListener
        {
            public static HashSet<string> BlacklistedSources = new() { "Unity", "Unity Log" };

            public CustomLogListener(string localPath, bool delayedFlushing = false)
            {
                DisplayedLogLevel = LogLevel.Info;

                FileStream fileStream;

                if (!BepInEx.Utility.TryOpenFileStream(Path.Combine(BepInEx.Paths.BepInExRootPath, localPath),
                    FileMode.Create, out fileStream, share: FileShare.Read, access: FileAccess.Write))
                {
                    Error($"couldn't open logfile {localPath}!");
                    return;
                }

                LogWriter = TextWriter.Synchronized(new StreamWriter(fileStream, BepInEx.Utility.UTF8NoBom));

                if (delayedFlushing) FlushTimer = new Timer(o => { LogWriter?.Flush(); }, null, 2000, 2000);

                InstantFlushing = !delayedFlushing;
            }

            public LogLevel DisplayedLogLevel { get; }

            public TextWriter LogWriter { get; protected set; }

            private Timer FlushTimer { get; }

            private bool InstantFlushing { get; }

            public LogLevel LogLevelFilter => DisplayedLogLevel;

            public int counter;

            public string StripTime(string line)
            {
                if (line.Length > 35 && line.Substring(8, 20) == "RainMeadow")
                {
                    for (int i = 0, count = 2; i < line.Length; i++)
                    {
                        if (line[i] == '|')
                        {
                            count--;
                            if (count == 0) return line.Substring(i + 1);
                        }
                    }
                }
                return line;
            }

            private string? lastStrippedLine;
            public bool LogEventIsRepeat(string line)
            {
                var origStrippedLine = lastStrippedLine;
                lastStrippedLine = StripTime(line);
                return origStrippedLine == lastStrippedLine;
            }

            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                if (LogWriter == null)
                    return;

                if (BlacklistedSources.Contains(eventArgs.Source.SourceName))
                    return;

                var line = eventArgs.ToString();
                if (LogEventIsRepeat(line))
                {
                    if (counter == 0)
                        LogWriter.WriteLine("... repeated");
                    counter++;
                }
                else
                {
                    if (counter > 0)
                        LogWriter.WriteLine($"... {counter} times");
                    counter = 0;
                    LogWriter.WriteLine(line);
                }

                if (InstantFlushing)
                    LogWriter.Flush();
            }

            public void Dispose()
            {
                FlushTimer?.Dispose();

                try
                {
                    LogWriter?.Flush();
                    LogWriter?.Dispose();
                }
                catch (ObjectDisposedException) { }
            }

            ~CustomLogListener()
            {
                Dispose();
            }
        }
    }
}
