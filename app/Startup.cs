using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Funq;
using ServiceStack;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using app.ServiceInterface;
using app.ServiceInterface.Config;
using System;
using System.Linq;
using ServiceStack.Logging;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Validation;
using app.Infrastructure;

namespace app
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration) => Configuration = configuration;
        public Autofac.IContainer ApplicationContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Added - uses IOptions<T> for your settings.
            services.AddOptions();

            // Added - Confirms that we have a home for our DemoSettings
            services.Configure<RnSettings>(Configuration.GetSection("app"));

            // Create the container builder.
            var builder = new ContainerBuilder();

            // Register dependencies, populate the services from
            // the collection, and build the container. If you want
            // to dispose of the container at the end of the app,
            // be sure to keep a reference to it as a property or field.
            //
            // Note that Populate is basically a foreach to add things
            // into Autofac that are in the collection. If you register
            // things in Autofac BEFORE Populate then the stuff in the
            // ServiceCollection can override those things; if you register
            // AFTER Populate those registrations can override things
            // in the ServiceCollection. Mix and match as needed.
            builder.Populate(services);
            //builder.RegisterType<MyType>().As<IMyType>();

            // Register All Controllers In The Current Scope
            //builder.RegisterControllers(AppDomain.CurrentDomain.GetAssemblies());

            var ourAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.Contains(nameof(app)))
                .ToList();
            //ourAssemblies.Add(typeof(UserService).Assembly);
            var fullnames = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName).ToList().OrderByDescending(x => x);
            var types = ourAssemblies.FirstNonDefault().DefinedTypes;
            // Register all services of xPortals as Single Instances
            builder.RegisterAssemblyTypes(ourAssemblies.ToArray())
                .Where(t => t.Name.EndsWith("Service", StringComparison.Ordinal))
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.Register(c =>
            {
                return c.Resolve<Microsoft.Extensions.Options.IOptions<RnSettings>>().Value;
            }).As<RnSettings>().SingleInstance();

            builder.Register<IDbConnectionFactory>(c =>
            {
                var settings = c.Resolve<RnSettings>();
                return new OrmLiteConnectionFactory(settings.Connections.Database, SqliteDialect.Provider);
            });

            ApplicationContainer = builder.Build();

            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseServiceStack(new AppHost(ApplicationContainer));
        }
    }

    public class AppHost : AppHostBase
    {
        private Autofac.IContainer autoFacContainer;
        public AppHost(Autofac.IContainer container) : base("app", typeof(MyServices).Assembly) 
        {
            autoFacContainer = container;
        }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            //Console.WriteLine
            LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);

            container.Register<IAuthRepository>(c =>
            new OrmLiteAuthRepository(container.Resolve<IDbConnectionFactory>())
            {
                UseDistinctRoleTables = true
            });

            //Create UserAuth RDBMS Tables
            container.Resolve<IAuthRepository>().InitSchema();

            //Also store User Sessions in SQL Server
            container.RegisterAs<OrmLiteCacheClient, ICacheClient>();
            container.Resolve<ICacheClient>().InitSchema();
            container.RegisterAs<OrmLiteAuthRepository, IUserAuthRepository>();

            //Register Autofac IoC container adapter, so ServiceStack can use it
            container.Adapter = new AutofacIocAdapter(autoFacContainer);

            //This method scans the assembly for validators
            //container.RegisterValidators(typeof(Api.General.RegistrationValidator).Assembly);

            SetConfig(new HostConfig { UseCamelCase = true });

            //Plugins.Add(new SwaggerFeature { UseBootstrapTheme = true });
            Plugins.Add(new PostmanFeature());
            Plugins.Add(new CorsFeature());

            //Plugins.Add(new RazorFormat());

            //Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
            //Plugins.Add(new AdminFeature());
            Plugins.Add(new ValidationFeature());

            //Plugins.Add(new RegistrationFeature());

            //Add Support for
            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[] {
                    new JwtAuthProvider(AppSettings) { AuthKey = AesUtils.CreateKey(),RequireSecureConnection=false },
                    new ApiKeyAuthProvider(AppSettings),        //Sign-in with API Key
                    new CredentialsAuthProvider(),              //Sign-in with UserName/Password credentials
                    new BasicAuthProvider(),                    //Sign-in with HTTP Basic Auth
                    new DigestAuthProvider(AppSettings),        //Sign-in with HTTP Digest Auth
                    new TwitterAuthProvider(AppSettings),       //Sign-in with Twitter
                    new FacebookAuthProvider(AppSettings),      //Sign-in with Facebook
                    //new YahooOpenIdOAuthProvider(AppSettings),  //Sign-in with Yahoo OpenId
                    //new OpenIdOAuthProvider(AppSettings),       //Sign-in with Custom OpenId
                    //new GoogleOAuth2Provider(AppSettings),      //Sign-in with Google OAuth2 Provider
                    //new LinkedInOAuth2Provider(AppSettings),    //Sign-in with LinkedIn OAuth2 Provider
                    //new GithubAuthProvider(AppSettings),        //Sign-in with GitHub OAuth Provider
                    //new YandexAuthProvider(AppSettings),        //Sign-in with Yandex OAuth Provider
                    //new VkAuthProvider(AppSettings),            //Sign-in with VK.com OAuth Provider
                }));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                //Create the PortalTempUser POCO table if it doesn't already exist
                //db.CreateTableIfNotExists<PortalTempUser>();
            }
        }
    }
}
