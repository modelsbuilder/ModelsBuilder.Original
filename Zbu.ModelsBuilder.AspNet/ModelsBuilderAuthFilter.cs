using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.Controllers;
using System.Web.Security;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models.Membership;

namespace Zbu.ModelsBuilder.AspNet
{
    public class ModelsBuilderAuthFilter : System.Web.Http.Filters.ActionFilterAttribute // use the http one, not mvc, with api controllers!
    {
        private static readonly char[] Separator = ":".ToCharArray();
        private readonly string _section;

        public ModelsBuilderAuthFilter(string section)
        {
            _section = section;
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                var user = Authenticate(actionContext.Request);
                if (user == null || !user.AllowedSections.Contains(_section))
                {
                    actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                //else
                //{
                //    // note - would that be a proper way to pass data to the controller?
                //    // see http://stevescodingblog.co.uk/basic-authentication-with-asp-net-webapi/
                //    actionContext.ControllerContext.RouteData.Values["umbraco-user"] = user;
                //}

                base.OnActionExecuting(actionContext);
            }
            catch
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        }

        private static IUser Authenticate(HttpRequestMessage request)
        {
            var ah = request.Headers.Authorization;
            if (ah == null || ah.Scheme != "Basic")
                return null;

            var token = ah.Parameter;
            var credentials = Encoding.ASCII
                .GetString(Convert.FromBase64String(token))
                .Split(Separator);
            if (credentials.Length != 2)
                return null;

            var username = credentials[0];
            var password = credentials[1];

#if UMBRACO_6
            var providerKey = umbraco.UmbracoSettings.DefaultBackofficeProvider;
#else
            var providerKey = UmbracoConfig.For.UmbracoSettings().Providers.DefaultBackOfficeUserProvider;
#endif
            var provider = Membership.Providers[providerKey];
            if (provider == null || !provider.ValidateUser(username, password))
                return null;
            var user = ApplicationContext.Current.Services.UserService.GetByUsername(username);
            if (!user.IsApproved || user.IsLockedOut)
                return null;
            return user;
        }
    }
}
