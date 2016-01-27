using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace Umbraco.ModelsBuilder.Api
{
    public class ModelsBuilderApiClient
    {
        private readonly string _url;
        private readonly string _user;
        private readonly string _password;

        private readonly JsonMediaTypeFormatter _formatter;
        private readonly MediaTypeFormatter[] _formatters;

        // fixme hardcoded?
        // could be options - but we cannot "discover" them as the API client runs outside of the web app
        // in addition, anything that references the controller forces API clients to reference Umbraco.Core
        private const string UmbracoOAuthTokenUrl = "/umbraco/oauth/token";
        private const string ApiControllerUrl = "/Umbraco/BackOffice/ModelsBuilder/ModelsBuilder/";

        public ModelsBuilderApiClient(string url, string user, string password)
        {
            _url = url.TrimEnd('/');
            _user = user;
            _password = password;

            _formatter = new JsonMediaTypeFormatter();
            _formatters = new MediaTypeFormatter[] { _formatter };
        }

        public void ValidateClientVersion()
        {
            // FIXME - add proxys support

            var hch = new HttpClientHandler();

            using (var client = new HttpClient(hch))
            {
                client.BaseAddress = new Uri(_url);
                Authorize(client);

                var data = new ValidateClientVersionData
                {
                    ClientVersion = ApiVersion.Current.Version,
                    MinServerVersionSupportingClient = ApiVersion.Current.MinServerVersionSupportingClient,
                };

                var result = client.PostAsync(_url + ApiControllerUrl + nameof(ModelsBuilderApiController.ValidateClientVersion),
                    data, _formatter).Result;

                // this is not providing enough details in case of an error - do our own reporting
                //result.EnsureSuccessStatusCode();
                EnsureSuccess(result);
            }
        }

        public IDictionary<string, string> GetModels(Dictionary<string, string> ourFiles, string modelsNamespace)
        {
            // FIXME - add proxys support

            var hch = new HttpClientHandler();

            //hch.Proxy = new WebProxy("path.to.proxy", 8888);
            //hch.UseProxy = true;

            using (var client = new HttpClient(hch))
            {
                client.BaseAddress = new Uri(_url);
                Authorize(client);

                var data = new GetModelsData
                {
                    Namespace = modelsNamespace,
                    ClientVersion = ApiVersion.Current.Version,
                    MinServerVersionSupportingClient = ApiVersion.Current.MinServerVersionSupportingClient,
                    Files = ourFiles
                };

                var result = client.PostAsync(_url + ApiControllerUrl + nameof(ModelsBuilderApiController.GetModels),
                    data, _formatter).Result;

                // this is not providing enough details in case of an error - do our own reporting
                //result.EnsureSuccessStatusCode();
                EnsureSuccess(result);

                var genFiles = result.Content.ReadAsAsync<IDictionary<string, string>>(_formatters).Result;
                return genFiles;
            }
        }

        private static void EnsureSuccess(HttpResponseMessage result)
        {
            if (result.IsSuccessStatusCode) return;

            var text = result.Content.ReadAsStringAsync().Result;
            throw new Exception($"Response status code does not indicate success ({result.StatusCode})\n{text}");
        }

        // fixme - for the time being, we don't cache the token and we auth on each API call
        private void Authorize(HttpClient client)
        {
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("userName", _user),
                new KeyValuePair<string, string>("password", _password),
            });

            var result = client.PostAsync(_url + UmbracoOAuthTokenUrl, formData).Result;

            EnsureSuccess(result);

            var token = result.Content.ReadAsAsync<TokenData>(_formatters).Result;
            if (token.TokenType != "bearer")
                throw new Exception($"Received invalid token type \"{token.TokenType}\", expected \"bearer\".");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        }
    }
}
