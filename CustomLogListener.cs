using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public class CustomLogListener : ILogListener
        {
            public static HashSet<string> BlacklistedSources = new() { "Unity", "Unity Log" };

            public CustomLogListener(string localPath, int maxLogFiles = 5, string? rootPath = null, bool delayedFlushing = false)
            {
                rootPath ??= BepInEx.Paths.BepInExRootPath;

                try
                {
                    var logFiles = Directory.GetFiles(rootPath, "meadowLog.*.log");
                    if (logFiles.Count() > maxLogFiles)
                    {
                        var i = logFiles.Count() - maxLogFiles;
                        foreach (var path in logFiles)
                        {
                            if (i == 0) break;
                            File.Delete(path);
                            i--;
                        }
                    }
                }
                catch (Exception e)
                {
                    Error($"couldn't clean up meadowLog files: {e}");
                }

                DisplayedLogLevel = LogLevel.Info;

                FileStream fileStream;

                if (!BepInEx.Utility.TryOpenFileStream(Path.Combine(rootPath, localPath),
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

            public string StripNumbers(string line)
            {
                return Regex.Replace(line, @"\d+", "{#}");
            }

            private int repeat;
            private Queue<Tuple<string,string>> lastLines = new();

            public bool LogEventIsRepeat(string line)
            {
                var stripped = StripNumbers(line);
                int i;
                for (i = lastLines.Count - 1; i >= 0; i--)
                    if (lastLines.ElementAt(i).Item2 == stripped)
                        break;
                if (repeat == 0 ? i >= 0 : i == 0)
                {
                    if (repeat == lastLines.Count)
                    {
                        LogWriter.WriteLine($"... last {lastLines.Count} lines repeated");
                        LogWriter.Flush();
                    }
                    while (i-- >= 0)
                        lastLines.Dequeue();
                    repeat++;
                }
                else if (repeat > 0)
                {
                    if (repeat > lastLines.Count)
                        LogWriter.WriteLine($"... {(int)repeat / lastLines.Count} times");
                    var remainder = repeat % lastLines.Count;
                    for (var j = lastLines.Count - remainder; j > 0; j--)
                        lastLines.Dequeue();
                    for (var k = remainder; k > 0; k--)
                        LogWriter.WriteLine(lastLines.Dequeue().Item1);
                    repeat = 0;
                }
                if (lastLines.Count > 10) lastLines.Dequeue();
                lastLines.Enqueue(Tuple.Create(line, stripped));
                return repeat > 0;
            }

            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                if (LogWriter == null)
                    return;

                if (BlacklistedSources.Contains(eventArgs.Source.SourceName))
                    return;

                var line = eventArgs.ToString();
                if (!LogEventIsRepeat(line))
                    LogWriter.WriteLine(line);

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
