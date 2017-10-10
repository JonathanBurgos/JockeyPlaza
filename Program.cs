using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace jockeyPlaza
{
    public class Program
    {
        public static void Main(string[] args)
        {            
            var host = new WebHostBuilder()
                .UseKestrel()
		        .UseUrls("http://127.0.0.1:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseSetting("detailedErrors", "true")
                .UseIISIntegration()
                .UseStartup<Startup>()
                .CaptureStartupErrors(true)
		        .Build();

            host.Run();
        }   
    }
}