﻿#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Vocaluxe.Base
{
    class CLogFile : IDisposable
    {
        private readonly string _LogFileName;
        private readonly string _LogName;
        private StreamWriter _LogFile;
        private readonly Object _FileMutex = new Object();

        public CLogFile(string fileName, string logName)
        {
            _LogName = logName;
            _LogFileName = fileName;
        }

        private void _Open()
        {
            lock (_FileMutex)
            {
                if (_LogFile != null)
                    return;
                _LogFile = new StreamWriter(Path.Combine(CSettings.DataPath, _LogFileName), false, Encoding.UTF8);

                _LogFile.WriteLine(_LogName + " " + CSettings.GetFullVersionText() + " " + DateTime.Now);
                _LogFile.WriteLine("----------------------------------------");
                _LogFile.WriteLine();
            }
        }

        public void Close()
        {
            lock (_FileMutex)
            {
                if (_LogFile == null)
                    return;
                try
                {
                    _LogFile.Flush();
                    _LogFile.Close();
                }
                catch (Exception) {}
            }
        }

        public virtual void Add(string text)
        {
            if (_LogFile == null)
                _Open();

            // ReSharper disable PossibleNullReferenceException
            _LogFile.WriteLine(text);
            // ReSharper restore PossibleNullReferenceException
            _LogFile.Flush();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
    }

    class CErrorLogFile : CLogFile
    {
        private int _NumErrors;
        private readonly Object _WriteMutex = new Object();

        public CErrorLogFile(string fileName, string logName) : base(fileName, logName) {}

        public override void Add(string errorText)
        {
            lock (_WriteMutex)
            {
                _NumErrors++;
                base.Add(_NumErrors + ") " + errorText);
            }
        }
    }

    static class CLog
    {
        private const int _MaxBenchmarks = 10;

        private static CLogFile _ErrorLog;
        private static CLogFile _PerformanceLog;
        private static CLogFile _BenchmarkLog;
        private static CLogFile _DebugLog;
        private static CLogFile _SongInfoLog;

        private static Stopwatch[] _BenchmarkTimer;
        private static readonly double _NanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;

        public static void Init()
        {
            _ErrorLog = new CErrorLogFile(CSettings.FileErrorLog, "Error-Log");
            _PerformanceLog = new CLogFile(CSettings.FilePerformanceLog, "Performance-Log");
            _BenchmarkLog = new CLogFile(CSettings.FileBenchmarkLog, "Benchmark-Log");
            _DebugLog = new CLogFile(CSettings.FileDebugLog, "Debug-Log");
            _SongInfoLog = new CLogFile(CSettings.FileSongInfoLog, "Song-Information-Log");

            _BenchmarkTimer = new Stopwatch[_MaxBenchmarks];
            for (int i = 0; i < _BenchmarkTimer.Length; i++)
                _BenchmarkTimer[i] = new Stopwatch();
        }

        public static void CloseAll()
        {
            _ErrorLog.Close();
            _PerformanceLog.Close();
            _BenchmarkLog.Close();
            _DebugLog.Close();
            _SongInfoLog.Close();
        }

        #region LogError
        public static void LogError(string errorText, bool show = false, bool exit = false, Exception e = null)
        {
            if (show)
                MessageBox.Show(errorText, CSettings.ProgramName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (e != null)
                errorText += ": " + e;
            _ErrorLog.Add(errorText);
            if (exit)
                Environment.Exit(Environment.ExitCode);
        }
        #endregion LogError

        public static void LogDebug(string text)
        {
            _DebugLog.Add(String.Format("{0:HH:mm:ss.ffff}", DateTime.Now) + ":" + text);
        }

        public static void LogSongInfo(string text)
        {
            _SongInfoLog.Add(text);
        }

        public static void LogPerformance(string text)
        {
            _PerformanceLog.Add(text);
            _PerformanceLog.Add("-------------------------------");
        }

        #region LogBenchmark
        public static void StartBenchmark(int benchmarkNr, string text)
        {
            if (benchmarkNr >= 0 && benchmarkNr < _MaxBenchmarks)
            {
                _BenchmarkTimer[benchmarkNr].Stop();
                _BenchmarkTimer[benchmarkNr].Reset();

                string space = String.Empty;
                for (int i = 0; i < benchmarkNr; i++)
                    space += "  ";
                _BenchmarkLog.Add(space + "Start " + text);

                _BenchmarkTimer[benchmarkNr].Start();
            }
        }

        public static void StopBenchmark(int benchmarkNr, string text)
        {
            if (benchmarkNr >= 0 && benchmarkNr < _MaxBenchmarks)
            {
                _BenchmarkTimer[benchmarkNr].Stop();

                string space = String.Empty;
                for (int i = 0; i < benchmarkNr; i++)
                    space += "  ";

                float ms;
                if (Stopwatch.IsHighResolution && _NanosecPerTick > 0)
                    ms = (float)((_NanosecPerTick * _BenchmarkTimer[benchmarkNr].ElapsedTicks) / (1000.0 * 1000.0));
                else
                    ms = _BenchmarkTimer[benchmarkNr].ElapsedMilliseconds;

                _BenchmarkLog.Add(space + "Stop " + text + ", Elapsed Time: " + ms.ToString("0.000") + "ms");
                _BenchmarkLog.Add(String.Empty);
            }
        }
        #endregion LogBenchmark
    }
}