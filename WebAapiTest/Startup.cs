using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using dotnet_etcd;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebAapiTest
{
    public class Startup
    {
        public static Uri uri = new Uri("http://127.0.0.1:5001");
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            IList<string> uriList = new List<string>();
            var controllers = Assembly.GetExecutingAssembly().GetTypes()
                 .Where(type => typeof(ControllerBase).IsAssignableFrom(type));
            Type routeType = typeof(RouteAttribute);
            foreach (var item in controllers)
            {
                string routeName = string.Empty;
                var route = item.CustomAttributes.Where(type => type.AttributeType.IsAssignableFrom(routeType));
                if (route.Count() > 0)
                {
                    routeName = route?.FirstOrDefault()?.ConstructorArguments?.FirstOrDefault().Value.ToString();
                    int inde = routeName.IndexOf("[");
                    routeName = routeName.Substring(0, inde);
                }
                string contName = item.Name.Replace("Controller", string.Empty);
                uriList.Add($"{contName}#{routeName}{contName}");
            }
            string host = "127.0.0.1";
            int port = 2379;
            string putKey = "/test/{0}#{1}";
            using (EtcdClient etcd = new EtcdClient(host, port))
            {
                foreach (var item in uriList)
                {
                    string[] uridata = item.Split('#');
                    etcd.PutAsync(string.Format(putKey, uridata[0], uri.Authority).ToLower(), $"{uri.ToString()}{uridata[1]}");
                }
            }
            lifetime.ApplicationStopped.Register(() =>
            {
                using (EtcdClient etcd = new EtcdClient(host, port))
                {
                    foreach (var item in uriList)
                    {
                        string[] uridata = item.Split('#');
                        etcd.DeleteAsync(string.Format(putKey, uridata[0], uri.Authority).ToLower());
                    }
                }
            });
            app.UseMvc();
        }
    }
}
