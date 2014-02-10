using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Nancy;
using Owin;
using Xunit;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace NancyOwinException
{
    public class Class1
    {
        [Fact]
        public async Task Should_handle_exceptions()
        {
            using (TestServer testServer = TestServer.Create<Startup>())
            {
                HttpResponseMessage response = await testServer.CreateRequest("/").GetAsync();
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            appBuilder
                .Use<CustomExceptionMiddleware>()
                .UseNancy();
        }
    }

    public class CustomExceptionMiddleware : OwinMiddleware
    {
        public CustomExceptionMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            try
            {
                await Next.Invoke(context);
            }
            catch (Exception ex)
            {
                //Stupid? yes, but we can't reach here.
                context.Response.StatusCode = 404; 
            }
        }
    }

    public class MyModule : NancyModule
    {
        public MyModule()
        {
            Get["/"] = _ => { throw new Exception("Derp!"); };
        }
    }
}