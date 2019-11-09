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
                                                 "Valor:A",
                                                 1,
                                                 When.NotExists);

            _redisClient.Db0.Database.HashSet(key,
                                     "Valor:B",
                                     1,
                                     When.NotExists);

            _redisClient.Db0.Database.HashSet(key,
                                      "Valor:C",
                                      1,
                                      When.NotExists);
            

            _redisClient.Db0.Database.HashSet(key,
                                             "Valor:A",
                                             100,
                                             When.Always,
                                             CommandFlags.FireAndForget);
            _redisClient.Db0.Database.HashSet(key,
                                             "Valor:A",
                                             200);

            _redisClient.Db0.Database.HashIncrement(key,
                                      "Valor:Incrementado",
                                      1, CommandFlags.FireAndForget);



            IRedisCacheConnectionPoolManager _redisCacheConnectionPoolManager = serviceProvider.GetService<IRedisCacheConnectionPoolManager>();
            var connection = _redisCacheConnectionPoolManager.GetConnection();
            IDatabase db = connection.GetDatabase();
            ISubscriber subscriber = connection.GetSubscriber();

            subscriber.Subscribe("Valor:*", (channel, message) =>
            {
                if ((string)channel == "Valor:Incrementado")
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd HH:mm:ss")}<Normal - {channel}><{message}>.");
                    // Do stuff if some item is added to a hypothethical "users" set in Redis
                }
            }
            );


            for (int i = 0; i < 100; i++)
            {
                _redisClient.Db0.Database.HashIncrement(key,
                                      "Valor:Incrementado",
                                      1, CommandFlags.FireAndForget);

                // get a publish client, or you can use connection.GetDatabase(), which won't open a new client.
                // GetSubscriber() will open a dedicated client which can only be used for Pub/Sub.
                var publisher = connection.GetSubscriber();
                var channelName = "Valor:*";
                // publish message to one channel
                publisher.Publish(channelName, $"Publish a message to literal channel: {channelName}");
                var channelName2 = "Valor:Incrementado";
                // publish message to one channel
                publisher.Publish(channelName2, $"Publish a message to literal channel: {channelName2}");
            }

            //_redisClient.Db0.Database.HashIncrement(key,
            //                          "ValorIncrementado",
            //                          1, CommandFlags.FireAndForget);


            


            Console.WriteLine("Hello World!");

            Console.ReadKey();


        }

        
    }
}
