﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Waf.Foundation;

using TumblThree.Applications;
using TumblThree.Applications.Auth;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;

namespace TumblThree.Presentation.DesignData
{
    public class MockShellService : Model, IShellService
    {
        public event CancelEventHandler Closing;
        public event EventHandler SettingsUpdatedHandler;

        public MockShellService() => Settings = new AppSettings();

        public AppSettings Settings { get; set; }

        public object ShellView { get; set; }

        public object ContentView { get; set; }

        public object DetailsView { get; set; }

        public object QueueView { get; set; }

        public object CrawlerView { get; set; }

        public IReadOnlyCollection<Task> TasksToCompleteBeforeShutdown { get; set; }

        public bool IsApplicationBusy { get; set; }

        public bool IsLongPathSupported { get; set; }

        public ClipboardMonitor ClipboardMonitor { get; set; }

        public OAuthManager OAuthManager { get; set; }

        public IDisposable SetApplicationBusy() => null;


        public void ShowError(Exception exception, string displayMessage)
        {
        }

        public void ShowDetailsView()
        {
        }

        public void ShowQueueView()
        {
        }

        public void UpdateDetailsView()
        {
        }

        public void AddTaskToCompleteBeforeShutdown(Task task)
        {
        }

        public void SettingsUpdated()
        {
        }

        protected virtual void OnClosing(CancelEventArgs e) => Closing?.Invoke(this, e);

        public bool CheckForWebView2Runtime()
        {
            throw new NotImplementedException();
        }
    }
}
