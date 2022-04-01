using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Redis.PruebasDeConcepto.Extensions;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Redis.PruebasDeConcepto
{
    class Program
    {
        static async void Main(string[] args)
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

            //Examplo
            var key = $"example:Eduardo:summary";
            _redisClient.Db0.Database.HashSet(key, "Valor:A", 1, When.NotExists);
            _redisClient.Db0.Database.HashSet(key, "Valor:B", 1, When.NotExists);
            _redisClient.Db0.Database.HashSet(key, "Valor:C", 1, When.NotExists);
            _redisClient.Db0.Database.HashSet(key, "Valor:A", 100, When.Always, CommandFlags.FireAndForget);
            _redisClient.Db0.Database.HashSet(key, "Valor:A", 200);


            // ******************EJEMPLO PARA ORDENES DE EVALUACION************************
            string OrdenId = Guid.NewGuid().ToString().ToLower();
            OrdenId = "f0cfdff6-194e-4351-8025-42697dc5f2ad";
            const int CANTIDAD_IMAGENES = 168;
            //Pub/Sub
            IRedisCacheConnectionPoolManager _redisCacheConnectionPoolManager = serviceProvider.GetService<IRedisCacheConnectionPoolManager>();
            var connection = _redisCacheConnectionPoolManager.GetConnection();
            IDatabase db = connection.GetDatabase();
            ISubscriber subscriber = connection.GetSubscriber();
            subscriber.Subscribe("status:*", (channel, message) =>
            {
                if ((string)channel == $"status:ord:{OrdenId}")
                {
                    //Print Avance
                    Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd HH:mm:ss")}<Normal - {channel}><{message}>.");

                    //Print cuando termine
                    var procesados = GetStringBetween((string)message,"[", "]").Split(',')[2];
                    if (Convert.ToInt32(procesados) == CANTIDAD_IMAGENES)
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd HH:mm:ss")}<Normal - {channel}><Se proceso todos las imagenes>.");
                    }
                }
            }
            );

            PruebaStatusOrdenEvaluacion(OrdenId, CANTIDAD_IMAGENES, _redisCacheConnectionPoolManager, _redisClient);
            
            var ordenId = Guid.NewGuid().ToString();
            PruebaCounters(ordenId,_redisClient);
            var X = await GetCountersOfRedis(ordenId, _redisClient);
            Console.ReadKey();
        }

        static void PruebaStatusOrdenEvaluacion(string ordenId, int cantidadImagenes, IRedisCacheConnectionPoolManager _redisCacheConnectionPoolManager, IRedisCacheClient _redisClient)
        {
            const string TotalRegistros = "tr";
            const string RegistrosProcesados = "rp";
            const string RegistrosValidos = "rv";
            const string RegistrosObservados = "ro";
            //  ord:d7687efb-feb4-4a4a-a51e-b110434571a6:status
            //          TotalRegistros          :   100
            //          RegistrosProcesados     :   40
            //          RegistrosValidos        :   38
            //          RegistrosObservados     :    2


            
            var key = $"ord:{ordenId}:status";

            _redisClient.Db0.Database.HashSet(key,
                                     TotalRegistros,
                                     cantidadImagenes,
                                     When.NotExists);


            var connection = _redisCacheConnectionPoolManager.GetConnection();
            // get a publish client, or you can use connection.GetDatabase(), which won't open a new client.
            // GetSubscriber() will open a dedicated client which can only be used for Pub/Sub.
            var publisher = connection.GetSubscriber();
            _redisClient.Db0.Database.KeyDelete(key);
            for (int i = 1; i <= cantidadImagenes; i++)
            {
                Thread.Sleep(50);
                //Redundante se podria sacar al sumar RegistrosValidos y RegistrosObservados
                long valorActual = _redisClient.Db0.Database.HashIncrement(key,
                                         RegistrosProcesados,
                                         1, CommandFlags.None);

                if (i % 10 == 0)
                {
                    //Marcar como errores los multiplos de 10
                    _redisClient.Db0.Database.HashIncrement(key,
                                      RegistrosObservados, 1, CommandFlags.FireAndForget);
                }
                else
                {
                    _redisClient.Db0.Database.HashIncrement(key,
                                      RegistrosValidos, 1, CommandFlags.FireAndForget);
                }

                //var channelName = "Status:*";
                //// publish message to one channel
                //publisher.Publish(channelName, $"Publish a message to literal channel: {channelName}");
                // publish message to one channel
                
                var idEntidad = "1";
                publisher.Publish($"status:ord:{ordenId}", $"[{idEntidad},{valorActual}]");

            }
        }

        static void PruebaCounters(string ordenId, IRedisCacheClient _redisClient)
        {
            //  ord:d7687efb-feb4-4a4a-a51e-b110434571a6:status
            //          TotalRegistros          :   100
            //          RegistrosProcesados     :   40
            //          RegistrosValidos        :   38
            //          RegistrosObservados     :    2

            var key = $"ord:{ordenId}:status";

            const string TotalRegistros = "tr";
            const string RegistrosProcesados = "rp";
            const string RegistrosValidos = "rv";
            const string RegistrosObservados = "ro";



            //Crea por primera vez el hash
            _redisClient.Db0.Database.HashSet(key, TotalRegistros, 100, When.NotExists);



            //Para incrementar uno de los campos  sin respuesta osea void
            _redisClient.Db0.Database.HashIncrement(key, RegistrosProcesados, 1, CommandFlags.FireAndForget);
            _redisClient.Db0.Database.HashIncrement(key, RegistrosValidos, 1, CommandFlags.FireAndForget);
            _redisClient.Db0.Database.HashIncrement(key, RegistrosObservados, 1, CommandFlags.FireAndForget);

            //Si incrementas y quieres que retorne el valor incrementado
            long valorActual = _redisClient.Db0.Database.HashIncrement(key,
                                                     RegistrosProcesados,
                                                     1, CommandFlags.None); 
        }
        //Para obtener los valores de un hash
        public static async Task<(int, int, int, int)> GetCountersOfRedis(string ordenId, IRedisCacheClient _redisClient)
        {
            var key = $"ord:{ordenId}:status";

            const string TotalRegistros = "tr";
            const string RegistrosProcesados = "rp";
            const string RegistrosValidos = "rv";
            const string RegistrosObservados = "ro";

            HashEntry[] hashValues = await _redisClient.GetDbFromConfiguration().Database.HashGetAllAsync(key);
            Dictionary<string, string> hashValuesDic = hashValues.ToDictionary(
                x => x.Name.ToString(),
                x => x.Value.ToString(),
                StringComparer.Ordinal);

            string cantidadRegistrosProcesados = hashValuesDic.ContainsKey(RegistrosProcesados) ? hashValuesDic[RegistrosProcesados] : "0";
            string cantidadRegistrosValidos = hashValuesDic.ContainsKey(RegistrosValidos) ? hashValuesDic[RegistrosValidos] : "0";
            string cantidadRegistrosObservados = hashValuesDic.ContainsKey(RegistrosObservados) ? hashValuesDic[RegistrosObservados] : "0";
            string cantidadTotalRegistros = hashValuesDic.ContainsKey(TotalRegistros) ? hashValuesDic[TotalRegistros] : "0";

            return (Convert.ToInt32(cantidadRegistrosProcesados),
                Convert.ToInt32(cantidadRegistrosValidos),
                Convert.ToInt32(cantidadRegistrosObservados),
                Convert.ToInt32(cantidadTotalRegistros));
        }

        private static string GetStringBetween(string message, string v1, string v2)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }
            var St = message.ToString();
            int pFrom = St.IndexOf("[") + "[".Length;
            int pTo = St.LastIndexOf("]");
            return St.Substring(pFrom, pTo - pFrom);
        }
    }
}
