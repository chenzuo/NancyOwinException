using System;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Nancy;
using Nancy.ErrorHandling;
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
                string body = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Equal("Derp!", body);
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
                //Stupid? yes. It's just an example.
                context.Response.StatusCode = 404;
                byte[] bytes = Encoding.UTF8.GetBytes(ex.Message);
                context.Response.Body.Write(bytes, 0, bytes.Length);
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

    public class RethrowStatusCodeHandler : IStatusCodeHandler
    {
        public bool HandlesStatusCode(Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            if (!context.Items.ContainsKey(NancyEngine.ERROR_EXCEPTION))
            {
                return false;
            }

            var exception = context.Items[NancyEngine.ERROR_EXCEPTION] as Exception;

            return statusCode == Nancy.HttpStatusCode.InternalServerError && exception != null;
        }

        public void Handle(Nancy.HttpStatusCode statusCode, NancyContext context)
        {
            Exception innerException = ((Exception) context.Items[NancyEngine.ERROR_EXCEPTION]).InnerException;
            ExceptionDispatchInfo
                .Capture(innerException)
                .Throw();
        }
    }
}