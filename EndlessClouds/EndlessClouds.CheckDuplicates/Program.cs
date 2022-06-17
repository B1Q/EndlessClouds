using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EndlessClouds.CheckDuplicates
{
    internal class Program
    {
        private static Random random = new Random();

        static async Task Main(string[] args)
        {
            var usersStorage = JsonConvert.DeserializeObject<List<T_User>>(File.Exists("users.json") ? await File.ReadAllTextAsync("users.json") : string.Empty);

            var database = new UserDatabase("Users", usersStorage);

            var random = new Random();
            var created = 0;

            var userNames = new T_User[30];

            Stopwatch watch = new Stopwatch();
            watch.Start();

            for (int i = 0; i < 1000000; ++i)
            {
                var userName = GetRandomString(random.Next(6, 30));
                if (database.TryCreateUser(userName, out var user))
                {
                    created++;
                    if (i < userName.Length) userNames[i] = user;
                }
                else
                {
                    // another attempt and done
                    userName = GetRandomString(random.Next(6, 30));
                    if (database.TryCreateUser(userName, out _)) created++;
                }
            }

            watch.Stop();
            Console.WriteLine($"Created: {created} Users in {watch.Elapsed.TotalMilliseconds}ms");

            watch.Restart();

            if (database.TryUpdateUser(userNames[0].AccountId, userNames[0].Username, GetRandomString(16)))
            {
                // user updated... do something
            }

            watch.Stop();
            Console.WriteLine($"Updated User {userNames[0].AccountId} in {watch.Elapsed.TotalMilliseconds}ms");

            var path = "Database/";

            if (!Directory.Exists(path))
                Directory.CreateDirectory($"{path}/data");

            await File.WriteAllTextAsync($"Database/{database.Name}.sql", database.GenerateSQLTable());

            var data = database.GenerateSQLData();
            for(int i=0; i < data.Length; ++i)
            {
                var fileName = $"{path}/data/{database.Name}_{i}.sql";

                await File.WriteAllTextAsync(fileName, data[i]);
            }

            Console.ReadLine();
        }

        /* Utility */
        internal static string GetRandomString(int stringLength)
        {
            StringBuilder sb = new StringBuilder();
            int numGuidsToConcat = (((stringLength - 1) / 32) + 1);
            for (int i = 1; i <= numGuidsToConcat; i++)
            {
                sb.Append(Guid.NewGuid().ToString("N"));
            }

            return sb.ToString(0, stringLength);
        }
    }
}
