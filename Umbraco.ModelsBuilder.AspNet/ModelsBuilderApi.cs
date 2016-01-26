using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;

namespace Umbraco.ModelsBuilder.AspNet
{
    public class ModelsBuilderApi
    {
        private readonly string _url;
        private readonly string _user;
        private readonly string _password;

        public ModelsBuilderApi(string url, string user, string password)
        {
            _url = url;
            _user = user;
            _password = password;
        }

        public static void EnsureSuccess(HttpResponseMessage result)
        {
            if (result.IsSuccessStatusCode) return;

            var text = result.Content.ReadAsStringAsync().Result;
            throw new Exception($"Response status code does not indicate success ({result.StatusCode})\n{text}");
        }

        public void ValidateClientVersion()
        {
            // FIXME - add proxys support

            var hch = new HttpClientHandler();

            using (var client = new HttpClient(hch))
            {
                var url = _url;
                client.BaseAddress = new Uri(url);
                if (url.EndsWith("/")) url = url.Substring(0, url.Length - 1);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(_user + ':' + _password)));

                var data = new ModelsBuilderController.ValidateClientVersionData
                {
                    ClientVersion = ApiVersion.Current.Version,
                    MinServerVersionSupportingClient = ApiVersion.Current.MinServerVersionSupportingClient,
                };

                var result = client.PostAsync(url + ModelsBuilderController.ActionUrl(nameof(ModelsBuilderController.ValidateClientVersion)),
                    data, new JsonMediaTypeFormatter()).Result;

                // this is not providing enough details in case of an error - do our own reporting
                //result.EnsureSuccessStatusCode();
                EnsureSuccess(result);
            }
        }

        //public IList<TypeModel> GetTypeModels()
        //{
        //    // FIXME - add proxys support

        //    var hch = new HttpClientHandler();
        //    //hch.Credentials = new NetworkCredential(user, password);

        //    //var cookies = new CookieContainer();
        //    //hch.CookieContainer = cookies;
        //    //hch.UseCookies = true;

        //    //hch.Proxy = new WebProxy("path.to.proxy", 8888);
        //    //hch.UseProxy = true;

        //    using (var client = new HttpClient(hch))
        //    {
        //        var url = _url;
        //        client.BaseAddress = new Uri(url);
        //        if (url.EndsWith("/")) url = url.Substring(0, url.Length - 1);

        //        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
        //            Convert.ToBase64String(Encoding.UTF8.GetBytes(_user + ':' + _password)));

        //        var result = client.GetAsync(url + ModelsBuilderApiController.GetTypeModelsUrl).Result;
        //        result.EnsureSuccessStatusCode();

        //        var formatters = new MediaTypeFormatter[] { new JsonMediaTypeFormatter() };
        //        return result.Content.ReadAsAsync<IList<TypeModel>>(formatters).Result;
        //    }
        //}

        public IDictionary<string, string> GetModels(Dictionary<string, string> ourFiles, string modelsNamespace)
        {
            // FIXME - add proxys support

            var hch = new HttpClientHandler();
            //hch.Credentials = new NetworkCredential(user, password);

            //var cookies = new CookieContainer();
            //hch.CookieContainer = cookies;
            //hch.UseCookies = true;

            //hch.Proxy = new WebProxy("path.to.proxy", 8888);
            //hch.UseProxy = true;

            using (var client = new HttpClient(hch))
            {
                var url = _url;
                client.BaseAddress = new Uri(url);
                if (url.EndsWith("/")) url = url.Substring(0, url.Length - 1);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(_user + ':' + _password)));

                var data = new ModelsBuilderController.GetModelsData
                {
                    Namespace = modelsNamespace,
                    ClientVersion = ApiVersion.Current.Version,
                    MinServerVersionSupportingClient = ApiVersion.Current.MinServerVersionSupportingClient,
                    Files = ourFiles
                };

                var result = client.PostAsync(url + ModelsBuilderController.ActionUrl(nameof(ModelsBuilderController.GetModels)),
                    data, new JsonMediaTypeFormatter()).Result;

                // this is not providing enough details in case of an error - do our own reporting
                //result.EnsureSuccessStatusCode();
                EnsureSuccess(result);

                var formatters = new MediaTypeFormatter[] { new JsonMediaTypeFormatter() };
                var genFiles = result.Content.ReadAsAsync<IDictionary<string, string>>(formatters).Result;
                return genFiles;
            }
        }
    }
}
