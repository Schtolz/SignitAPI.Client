using System.Web.Http;

namespace SignitIntegrationSample
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/v1/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            

            /*config.Routes.MapHttpRoute(
                name: "CreateOrder",
                routeTemplate: "api/SignitIntegrationApi/PostCreateOrder",
                defaults: new { id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "PostRequestToken",
                routeTemplate: "api/SignitIntegrationApi/RequestToken",
                defaults: new { id = RouteParameter.Optional }
            );*/
        }
    }
}
