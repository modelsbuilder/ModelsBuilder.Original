using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder.AspNet
{
    public class ModelsBuilderApi
    {
        private string _url;
        private string _user;
        private string _password;

        public ModelsBuilderApi(string url, string user, string password)
        {
            _url = url;
            _user = user;
            _password = password;
        }

        public IList<TypeModel> GetTypeModels()
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

                var result = client.GetAsync(url + ModelsBuilderApiController.GetTypeModelsUrl).Result;
                result.EnsureSuccessStatusCode();

                var formatters = new MediaTypeFormatter[] { new JsonMediaTypeFormatter() };
                return result.Content.ReadAsAsync<IList<TypeModel>>(formatters).Result;
            }
        }

        public IDictionary<string, string> GetModels(IDictionary<string, string> ourFiles, string modelsNamespace)
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

                ourFiles["__META__"] = modelsNamespace;
                var result = client.PostAsync(url + ModelsBuilderApiController.GetModelsUrl, ourFiles, new JsonMediaTypeFormatter()).Result;

                // this is not providing enough details in case of an error - do our own reporting
                //result.EnsureSuccessStatusCode();
                if (!result.IsSuccessStatusCode)
                {
                    var text = result.Content.ReadAsStringAsync().Result;
                    throw new Exception(string.Format("Response status code does not indicate success ({0})\n{1}",
                        result.StatusCode, text));
                }

                var formatters = new MediaTypeFormatter[] { new JsonMediaTypeFormatter() };
                var genFiles = result.Content.ReadAsAsync<IDictionary<string, string>>(formatters).Result;
                return genFiles;
            }
        }
    }
}
