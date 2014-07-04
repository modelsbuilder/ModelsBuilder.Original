using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using Zbu.ModelsBuilder.Building;

namespace Zbu.ModelsBuilder.AspNet
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
            throw new Exception(string.Format("Response status code does not indicate success ({0})\n{1}",
                result.StatusCode, text));
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

                var clientVersion = typeof(TypeModel).Assembly.GetName().Version;
                var result = client.PostAsync(url + ModelsBuilderApiController.ValidateClientVersionUrl, clientVersion, new JsonMediaTypeFormatter()).Result;

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

                var data = new ModelsBuilderApiController.GetModelsData
                {
                    Namespace = modelsNamespace,
                    ClientVersion = typeof (TypeModel).Assembly.GetName().Version,
                    Files = ourFiles
                };

                var result = client.PostAsync(url + ModelsBuilderApiController.GetModelsUrl, data, new JsonMediaTypeFormatter()).Result;

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
