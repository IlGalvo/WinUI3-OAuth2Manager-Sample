using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;

namespace Client
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MyButton_Click(object sender, RoutedEventArgs e)
        {
            var isPackaged = !Utilities.IsPackaged();

            try
            {
                if (isPackaged)
                {
                    ActivationRegistrationManager.RegisterForProtocolActivation("ms-testoauthcsharp-launch", "Assets\\Square150x150Logo.scale-100", "My App Name", null);
                }

                var authParams = AuthRequestParams.CreateForAuthorizationCodeRequest("WinUI3TestClient", new Uri("ms-testoauthcsharp-launch://oauthcallback"));
                authParams.Scope = "openid profile email";

                // Richiedi autorizzazione
                var authResult = await OAuth2Manager.RequestAuthWithParamsAsync(AppWindow.Id, new Uri("https://localhost:7088/connect/authorize"), authParams);

                if (authResult.Response is not null)
                {
                    myText.Text = authResult.ResponseUri.ToString();
                }
            }
            catch (Exception ex)
            {
                myText.Text = ex.ToString();
            }
            finally
            {
                if (isPackaged)
                {
                    ActivationRegistrationManager.UnregisterForProtocolActivation("ms-testoauthcsharp-launch", null);
                }
            }
        }
    }
}