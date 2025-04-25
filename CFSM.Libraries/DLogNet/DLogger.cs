using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

// Usage: see the ReadMe.txt file

namespace DLogNet
{
    public class DLogger : IDisposable
    {
        /// <summary>
        /// The complete collection of log messages sent to and processed by this class.
        /// </summary>
        private List<DLogMessage> logEntries = new List<DLogMessage>();
        private List<TextBox> targetTextBoxes = new List<TextBox>();
        private List<FileInfo> targetFiles = new List<FileInfo>();
        private List<NotifyIcon> targetNotifyIcons = new List<NotifyIcon>();
        private List<ProgressBar> targetProgressBars = new List<ProgressBar>();
        private List<ToolStripProgressBar> targetToolStripProgressBars = new List<ToolStripProgressBar>();

        /// <summary>
        /// The collection used to store log messages before they are processed by this class.
        /// Outside threads can add messages to this collection, and this class will process them in a thread-safe manner.
        /// </summary>
        private BlockingCollection<LogMessageWithProgress> _logQueue = new BlockingCollection<LogMessageWithProgress>();

        /// <summary>
        /// The thread that processes log messages from the queue and writes them to the target controls.
        /// </summary>
        private Thread _processingThread = null;

        /// <summary>
        /// Flag to indicate whether the processing thread is currently running.
        /// </summary>
        private bool _processingThreadRunning = false;

        public void Log(string message, int progress = -1)
        {
#if DEBUG
            // writes log message to output window when in Debug mode for developers
            Debug.WriteLine(message);
#endif
            Write(message, progress);
        }

        private NotifyIcon _notifyIcon;
        public NotifyIcon Notifier
        {
            get { return _notifyIcon ?? (_notifyIcon = new NotifyIcon()); }
            set { _notifyIcon = value; }
        }

        public List<NotifyIcon> TargetNotifyIcons
        {
            get { return targetNotifyIcons; }
        }

        public List<FileInfo> TargetFiles
        {
            get { return targetFiles; }
        }

        public List<TextBox> TargetTextBoxes
        {
            get { return targetTextBoxes; }
        }

        public List<ProgressBar> TargetProgressBars
        {
            get { return targetProgressBars; }
        }

        public List<ToolStripProgressBar> TargetToolStripProgressBars
        {
            get { return targetToolStripProgressBars; }
        }

        /// <summary>
        /// Instanciates Log class
        /// </summary>
        public DLogger()
        {
            startProcessingThread();
        }

        private class LogMessageWithProgress
        {
            public string Message { get; set; }
            public int Progress { get; set; }
            public LogMessageWithProgress(string message, int progress)
            {
                Message = message;
                Progress = progress;
            }
        }

        private void startProcessingThread()
        {
            // Define the processing thread's behavior
            _processingThread = new Thread(() =>
            {
                LogMessageWithProgress msg = null;
                while (_processingThreadRunning)
                {
                    // Reset the message object
                    msg = null;

                    // Try to take a message from the queue
                    try
                    {
                        msg = _logQueue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                        // The collection has been marked as complete
                        // => Do nothing, as we are never going to mark the collection as complete
                    }

                    // If we have a message, process it
                    if (msg != null)
                    {
                        // Process the message (e.g., write to target controls)
                        processLogMessage(msg);
                    }
                }
            });

            // Set the flag to indicate that the processing thread is running
            _processingThreadRunning = true;

            // Start the processing thread
            _processingThread.Start();
        }

        /// <summary>
        /// Adds TextBox control for log output target control list
        /// </summary>
        /// <param name="textBox">TextBox to add</param>
        public void AddTargetTextBox(TextBox textBox)
        {
            if (targetTextBoxes == null)
                targetTextBoxes = new List<TextBox>();
            targetTextBoxes.Add(textBox);
        }

        /// <summary>
        /// Removes TextBox control from log output
        /// </summary>
        /// <param name="textBox">TextBox to remove</param>
        public void RemoveTargetTextBox(TextBox textBox)
        {
            if (targetTextBoxes.Contains(textBox))
                targetTextBoxes.Remove(textBox);
        }

        /// <summary>
        /// Adds file by path for log output target file list
        /// </summary>
        /// <param name="path">Path of the file</param>
        public void AddTargetFile(string path)
        {
            if (targetFiles == null)
                targetFiles = new List<FileInfo>();
            FileInfo newTargetFile = new FileInfo(path);

            if (newTargetFile.Directory != null && !newTargetFile.Directory.Exists)
                Directory.CreateDirectory(newTargetFile.Directory.FullName);

            // commented out ... not a good idea to do this in middle of a process
            // automatically delete and recreate log file when it gets too big
            //if (newTargetFile.Exists && newTargetFile.Length / 1024 > 1024)
            //    File.Delete(path);

            if (!newTargetFile.Exists)
            {
                StreamWriter sw = File.CreateText(newTargetFile.FullName);
                sw.Flush();
                sw.Close();
            }

            bool exists = false;
            foreach (FileInfo targetFile in targetFiles)
            {
                if (targetFile.FullName == path)
                    exists = true;
            }

            if (!exists)
                targetFiles.Add(newTargetFile);
        }

        /// <summary>
        /// Removes file from log output
        /// </summary>
        /// <param name="path">Log file path</param>
        public void RemoveTargetFile(string path)
        {
            foreach (FileInfo targetFile in targetFiles.ToList())
            {
                if (targetFile.FullName == path)
                    targetFiles.Remove(targetFile);
            }
        }

        /// <summary>
        /// Adds NotifyIcon control for log output 
        /// </summary>
        /// <param name="notifyIcon">NotifyIcon to add</param>
        public void AddTargetNotifyIcon(NotifyIcon notifyIcon)
        {
            if (targetNotifyIcons == null)
                targetNotifyIcons = new List<NotifyIcon>();
            targetNotifyIcons.Add(notifyIcon);
        }

        /// <summary>
        /// Removes Notify Icon control from log output
        /// </summary>
        /// <param name="notifyIcon">Notify Icon control to remove</param>
        public void RemoveTargetNotifyIcon(NotifyIcon notifyIcon)
        {
            if (targetNotifyIcons.Contains(notifyIcon))
                targetNotifyIcons.Remove(notifyIcon);
        }

        /// <summary>
        /// Adds file by FileInfo type variable for log output target file list
        /// </summary>
        /// <param name="file">FileInfo type variable</param>
        public void AddTargetFile(FileInfo file)
        {
            AddTargetFile(file.FullName);
        }

        /// <summary>
        /// Adds Progress Bar control for log output
        /// </summary>
        /// <param name="progressBar">Progress Bar control to add</param>
        public void AddProgressBar(ProgressBar progressBar)
        {
            if (targetProgressBars == null)
                targetProgressBars = new List<ProgressBar>();
            targetProgressBars.Add(progressBar);
        }

        /// <summary>
        /// Removes Progress Bar control from log output
        /// </summary>
        /// <param name="progressBar">Progress Bar control to remove</param>
        public void RemoveTargetProgressBar(ProgressBar progressBar)
        {
            if (targetProgressBars.Contains(progressBar))
                targetProgressBars.Remove(progressBar);
        }

        /// <summary>
        /// Adds ToolStripProgressBar control for log output
        /// </summary>
        /// <param name="progressBar">ToolStripProgressBar control to add</param>
        public void AddToolStripProgressBar(ToolStripProgressBar progressBar)
        {
            if (targetToolStripProgressBars == null)
                targetToolStripProgressBars = new List<ToolStripProgressBar>();
            targetToolStripProgressBars.Add(progressBar);
        }

        /// <summary>
        /// Removes ToolStripProgressBar control from log output
        /// </summary>
        /// <param name="progressBar">ToolStripProgressBar control to remove</param>
        public void RemoveTargetProgressBar(ToolStripProgressBar progressBar)
        {
            if (targetToolStripProgressBars.Contains(progressBar))
                targetToolStripProgressBars.Remove(progressBar);
        }

        /// <summary>
        /// Writes the given string to the logger queue, to then be processed and written to the targets.
        /// </summary>
        /// <param name="message">Log message</param>
        /// <param name="progress">(Optional) Progress value (for ProgressBar type controls)</param>
        public void Write(string message, int progress = -1)
        {
            // Queue the message
            _logQueue.Add(new LogMessageWithProgress(message, progress));

            // We return after the above call
            // The lines below are how the method was before we added the queue

            /*
            DLogMessage msg = new DLogMessage(message);
            
            logEntries.Add(msg);

            try
            {
                foreach (var entry in logEntries)
                {
                    if (targetTextBoxes != null)
                    {
                        foreach (Control control in targetTextBoxes)
                        {
                            Control myControl = control;
                            DLogMessage myLogEntry = entry;
                            control.InvokeIfRequired(delegate
                                {
                                    myControl.Text += myLogEntry.GetFormatted() + Environment.NewLine;
                                    if (myControl is TextBox)
                                    {
                                        ((TextBox)myControl).SelectionStart = ((TextBox)myControl).TextLength;
                                        ((TextBox)myControl).ScrollToCaret();
                                    }
                                });
                        }
                    }

                    if (targetFiles != null)
                    {
                        foreach (FileInfo targetFile in targetFiles)
                        {
                            // workaround FileInfo sometimes returns false when file exists
                            var targetPath = Path.Combine(targetFile.DirectoryName, targetFile.Name);
                            if (!File.Exists(targetPath)) // targetFile.Exists))
                            {
                                using (StreamWriter sw = targetFile.CreateText())
                                {
                                    sw.WriteLine("Log started");
                                    sw.Flush();
                                    sw.Close();
                                }
                            }

                            using (StreamWriter sw = targetFile.AppendText())
                            {
                                sw.WriteLine(entry.GetFormatted());
                                sw.Flush();
                                sw.Close();
                            }
                        }
                    }

                    if (targetNotifyIcons != null)
                    {
                        foreach (NotifyIcon notifyIcon in targetNotifyIcons)
                        {
                            ToolTipIcon icon;
                            notifyIcon.BalloonTipText = entry.GetFormatted();
                            notifyIcon.Visible = true;
                            if (entry.Message == null) continue;
                            if (entry.Message.ToLower().Contains("error"))
                                icon = ToolTipIcon.Error;
                            else
                                icon = ToolTipIcon.Info;

                            notifyIcon.ShowBalloonTip(1, "Information", entry.Message, icon);
                        }
                    }

                    if (progress > -1)
                    {
                        if (targetProgressBars != null)
                        {
                            foreach (ProgressBar progressBar in targetProgressBars)
                            {
                                ProgressBar bar = progressBar;
                                bar.InvokeIfRequired(delegate
                                    { bar.Value = progress; });
                            }
                        }

                        if (targetToolStripProgressBars != null)
                        {
                            foreach (ToolStripProgressBar toolStripProgressBar in targetToolStripProgressBars)
                            {
                                toolStripProgressBar.Value = progress;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // some intermitent error shows up here
                Debug.Write("DLogger: " + ex.Message);
            }

            logEntries = new List<DLogMessage>();
            */
        }

        /// <summary>
        /// Processes the log message and writes it to the target controls.
        /// Used by the thread that is handled by this class.
        /// </summary>
        /// <param name="message"></param>
        private void processLogMessage(LogMessageWithProgress message)
        {
            // Convert the message to a DLogMessage object
            DLogMessage msg = new DLogMessage(message.Message);

            // Add the message to the log entries list
            logEntries.Add(msg);

            try
            {
                if (targetTextBoxes != null)
                {
                    foreach (Control control in targetTextBoxes)
                    {
                        Control myControl = control;
                        DLogMessage myLogEntry = msg;
                        control.InvokeIfRequired(delegate
                        {
                            myControl.Text += myLogEntry.GetFormatted() + Environment.NewLine;
                            if (myControl is TextBox)
                            {
                                ((TextBox)myControl).SelectionStart = ((TextBox)myControl).TextLength;
                                ((TextBox)myControl).ScrollToCaret();
                            }
                        });
                    }
                }

                if (targetFiles != null)
                {
                    foreach (FileInfo targetFile in targetFiles)
                    {
                        // workaround FileInfo sometimes returns false when file exists
                        var targetPath = Path.Combine(targetFile.DirectoryName, targetFile.Name);
                        if (!File.Exists(targetPath)) // targetFile.Exists))
                        {
                            using (StreamWriter sw = targetFile.CreateText())
                            {
                                sw.WriteLine("Log started");
                                sw.Flush();
                                sw.Close();
                            }
                        }

                        using (StreamWriter sw = targetFile.AppendText())
                        {
                            sw.WriteLine(msg.GetFormatted());
                            sw.Flush();
                            sw.Close();
                        }
                    }
                }

                if (targetNotifyIcons != null)
                {
                    foreach (NotifyIcon notifyIcon in targetNotifyIcons)
                    {
                        ToolTipIcon icon;
                        notifyIcon.BalloonTipText = msg.GetFormatted();
                        notifyIcon.Visible = true;
                        if (msg.Message == null) continue;
                        if (msg.Message.ToLower().Contains("error"))
                            icon = ToolTipIcon.Error;
                        else
                            icon = ToolTipIcon.Info;

                        notifyIcon.ShowBalloonTip(1, "Information", msg.Message, icon);
                    }
                }

                if (message.Progress > -1)
                {
                    if (targetProgressBars != null)
                    {
                        foreach (ProgressBar progressBar in targetProgressBars)
                        {
                            ProgressBar bar = progressBar;
                            bar.InvokeIfRequired(delegate
                            { bar.Value = message.Progress; });
                        }
                    }

                    if (targetToolStripProgressBars != null)
                    {
                        foreach (ToolStripProgressBar toolStripProgressBar in targetToolStripProgressBars)
                        {
                            toolStripProgressBar.Value = message.Progress;
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                // some intermitent error shows up here
                Debug.Write("DLogger: " + ex.Message);

                // Write to log
                _logQueue.Add(new LogMessageWithProgress("Error: " + ex.Message, -1));
            }
        }

        public void Dispose()
        {
            logEntries = null;
            targetTextBoxes = null;
            targetFiles = null;
            targetNotifyIcons = null;
            targetProgressBars = null;
            targetToolStripProgressBars = null;
        }
    }
}
