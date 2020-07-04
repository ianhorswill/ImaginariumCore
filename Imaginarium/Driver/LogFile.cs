#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogFile.cs" company="Ian Horswill">
// Copyright (C) 2019, 2020 Ian Horswill
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion

using System;
using System.IO;

namespace Imaginarium.Driver
{
    /// <summary>
    /// Log that can be used for generating bug reports.
    /// </summary>
    public static class LogFile
    {
        private static string LogFilePath =>
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + Path.DirectorySeparatorChar +
            "Imaginarium log.txt";

        private static TextWriter logFile;
        private static bool handlersInstalled;

        /// <summary>
        /// Flush the log file in case there's a crash
        /// </summary>
        public static void Flush()
        {
            logFile?.FlushAsync();
        }

        /// <summary>
        /// True if we're currently logging.
        /// </summary>
        public static bool Enabled
        {
            get => logFile != null;
            set
            {
                if (value != Enabled)
                {
                    if (value)
                    {
                        logFile = File.CreateText(LogFilePath);
                        logFile.WriteLine($"Debugging log created {DateTime.Now}");
                        logFile.WriteLine();
                        separated = true;
                        if (!handlersInstalled)
                        {
#if notdef
                        Application.quitting += () =>
                        {
                            logFile?.Close();
                            logFile = null;
                        };
                        Application.logMessageReceived += (condition, trace, type) =>
                        {
                            if (logFile != null)
                            {
                                logFile.WriteLine($"{type}: {condition}");
                                logFile.WriteLine(trace);
                            }
                        };
#endif
                            handlersInstalled = true;
                        }
                    }
                    else
                    {
                        logFile.Close();
                        logFile = null;
                    }
                }
            }
        }

        private static bool separated;

        /// <summary>
        /// Write a message to the log file
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            logFile?.WriteLine(message);
            separated = false;
        }

        /// <summary>
        /// Write a message to the log file
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void Log(string format, params object[] args)
        {
            logFile?.WriteLine(format, args);
            separated = false;
        }

        /// <summary>
        /// Write a blank line to the log file
        /// </summary>
        public static void Separate()
        {
            if (!separated)
            {
                logFile?.WriteLine();
                separated = true;
            }
        }
    }
}
