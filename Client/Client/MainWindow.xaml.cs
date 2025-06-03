using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

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

                myText.Text = "Starting OAUth2 flow...";

                var authUri = new Uri("https://localhost:7088/connect/authorize");

                var authRequestParams = AuthRequestParams.CreateForAuthorizationCodeRequest("WinUI3TestClientId", new Uri("ms-testoauthcsharp-launch://oauthcallback/"));
                authRequestParams.Scope = "openid profile email";

                var authRequestResult = await OAuth2Manager.RequestAuthWithParamsAsync(AppWindow.Id, authUri, authRequestParams);

                if (authRequestResult.Response is null)
                {
                    myText.Text = $"Errore in fase di autorizzazione: {authRequestResult.Failure.Error}, {authRequestResult.Failure.ErrorDescription}";
                    return;
                }

                myText.Text = $"Ricevuto authorization code: {authRequestResult.Response.Code}\nProcedo con RequestTokenAsync di OAuth2Manager…";

                var tokenUri = new Uri("https://localhost:7088/connect/token");
                var tokenRequestParams = TokenRequestParams.CreateForAuthorizationCodeRequest(authRequestResult.Response);

                ClientAuthentication clientAuth = ClientAuthentication.CreateForBasicAuthorization("WinUI3TestClientId", "WinUI3TestClientSecret");

                var tokenRequestResult = await OAuth2Manager.RequestTokenAsync(tokenUri, tokenRequestParams, clientAuth);

                if (tokenRequestResult.Response is null)
                {
                    myText.Text = $"Errore in fase di richiesta token: {tokenRequestResult.Failure.Error}, {tokenRequestResult.Failure.ErrorDescription}";
                    return;
                }

                myText.Text = $"Ricevuto access token: {tokenRequestResult.Response.AccessToken}\n";

                var userInfo = await GetUserInfoAsync("https://localhost:7088/connect/userinfo", tokenRequestResult.Response.AccessToken);

                if(userInfo is null)
                {
                    myText.Text = "Errore durante la richiesta UserInfo.";
                    return;
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var userInfoJson = JsonSerializer.Serialize(userInfo, options);

                myText.Text = $"UserInfo:\n{userInfoJson}";
            }
            catch (Exception exception)
            {
                myText.Text = exception.ToString();
            }
            finally
            {
                if (isPackaged)
                {
                    ActivationRegistrationManager.UnregisterForProtocolActivation("ms-testoauthcsharp-launch", null);
                }
            }
        }

        private async Task<JsonElement?> GetUserInfoAsync(string userInfoEndpoint, string accessToken)
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await http.GetAsync(userInfoEndpoint);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var doc = JsonDocument.Parse(body);
                return doc.RootElement;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}