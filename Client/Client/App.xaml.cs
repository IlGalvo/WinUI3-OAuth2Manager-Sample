using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Activation;

namespace Client
{
    public partial class App : Application
    {
        private Window? m_window;

        public App()
        {
            InitializeComponent();

            var mainInstance = AppInstance.FindOrRegisterForKey("MainInstance");
            if (!mainInstance.IsCurrent)
            {
                var args = AppInstance.GetCurrent().GetActivatedEventArgs();
                mainInstance.RedirectActivationToAsync(args).GetAwaiter().GetResult();

                Process.GetCurrentProcess().Kill();
                return;
            }

            mainInstance.Activated += OnAppInstanceActivated;
        }

        private void OnAppInstanceActivated(object? sender, AppActivationArguments e)
        {
            if (e.Kind == ExtendedActivationKind.Protocol && e.Data is IProtocolActivatedEventArgs protocolArgs)
            {
                var callbackUri = protocolArgs.Uri;

                if (callbackUri.Authority != "oauthcallback" || !OAuth2Manager.CompleteAuthRequest(callbackUri))
                {
                    Process.GetCurrentProcess().Kill();
                    return;
                }

                Utilities.ShowWindowForeground(m_window!);
            }
        }
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}