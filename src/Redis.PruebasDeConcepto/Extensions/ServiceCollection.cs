using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;
using StackExchange.Redis.Extensions.Utf8Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Redis.PruebasDeConcepto.Extensions
{
    public static class ServiceCollectionExtensions
    {

        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {

            var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();
            services.AddSingleton(redisConfiguration);
            services.AddSingleton<IRedisClientFactory, RedisClientFactory>();
            services.AddSingleton<IRedisClient>((provider) => provider
              .GetRequiredService<IRedisClientFactory>()
              .GetDefaultRedisClient());
            services.AddSingleton<IRedisDatabase>((provider) => provider
              .GetRequiredService<IRedisClientFactory>()
              .GetDefaultRedisClient()
              .GetDefaultDatabase());
            services.AddSingleton<ISerializer, Utf8JsonSerializer>();
            return services;

        }

    }
}
