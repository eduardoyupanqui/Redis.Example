using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Redis.PruebasDeConcepto.Extensions;
using System;

namespace Redis.PruebasDeConcepto
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
             .AddJsonFile("appsettings.json", true, true)
             .Build();
            IServiceCollection services = new ServiceCollection();
            services.AddRedis(config);



            Console.WriteLine("Hello World!");




        }

        
    }
}
