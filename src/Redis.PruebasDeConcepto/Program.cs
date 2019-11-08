using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Redis.PruebasDeConcepto.Extensions;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;

namespace Redis.PruebasDeConcepto
{
    class Program
    {
        static void Main(string[] args)
        {
            //var builder = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("appsettings.json");

            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            
            IServiceCollection services = new ServiceCollection();
            services.AddRedis(config);

            var serviceProvider = services.BuildServiceProvider();
            IRedisCacheClient _redisClient = serviceProvider.GetService<IRedisCacheClient>();


            var key = $"example:Eduardo:summary";
            _redisClient.Db0.Database.HashSet(key,
                                                 "ValorA",
                                                 1,
                                                 When.NotExists);

            _redisClient.Db0.Database.HashSet(key,
                                     "ValorB",
                                     1,
                                     When.NotExists);

            _redisClient.Db0.Database.HashSet(key,
                                      "ValorC",
                                      1,
                                      When.NotExists);
            

            _redisClient.Db0.Database.HashSet(key,
                                             "ValorA",
                                             100,
                                             When.Always,
                                             CommandFlags.FireAndForget);
            _redisClient.Db0.Database.HashSet(key,
                                             "ValorA",
                                             200);

            for (int i = 0; i < 100; i++)
            {
                _redisClient.Db0.Database.HashIncrement(key,
                                      "ValorIncrementado",
                                      1, CommandFlags.FireAndForget);
            }
            
            //_redisClient.Db0.Database.HashIncrement(key,
            //                          "ValorIncrementado",
            //                          1, CommandFlags.FireAndForget);


            Console.WriteLine("Hello World!");

            Console.ReadKey();


        }

        
    }
}
