using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EndlessClouds
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var tokensStored = JsonConvert.DeserializeObject<List<T_Token>>(File.Exists("store.json") ? await File.ReadAllTextAsync("store.json") : string.Empty);

            var database = new Database("TokensDatabase", tokensStored);

            var user = Guid.NewGuid();

            /* a few initial calls to speed things up */
            database.GenerateToken(Guid.NewGuid());
            database.GenerateToken(Guid.NewGuid());
            database.GenerateToken(Guid.NewGuid());

            database.GenerateToken(user);
            
            var token = database.GetToken(user);
            Console.WriteLine(token);

            while (!token.TokenExpired)
            {
                Console.WriteLine($"Token has not expired yet!");
                Thread.Sleep(500);
            }

            Console.WriteLine($"Token {token.Token}[{user.ToString()}] has Expired!");

            Console.WriteLine($"Generating 100 Tokens!");

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 100; ++i)
                database.GenerateToken(user);

            watch.Stop();
            Console.WriteLine($"Generating 100 tokens took {watch.Elapsed.TotalMilliseconds} ms");
            Console.WriteLine($"Retrieve 100 Tokens for user: {database.GetTokens(user, 100)?.Count}");


            /* Generate SQL Database */
            var path = "Database/";

            if (!Directory.Exists(path))
                Directory.CreateDirectory($"{path}/data");

            await File.WriteAllTextAsync($"Database/{database.Name}.sql", database.GenerateSQLTable());

            var data = database.GenerateSQLData();
            for (int i = 0; i < data.Length; ++i)
            {
                var fileName = $"{path}/data/{database.Name}_{i}.sql";

                await File.WriteAllTextAsync(fileName, data[i]);
            }

            Console.ReadLine();

            Console.ReadLine();

            // don't use Indented in production
            //await File.WriteAllTextAsync("store.json", JsonConvert.SerializeObject(database.TokensGenerated, Formatting.Indented));

        }

    }
}
