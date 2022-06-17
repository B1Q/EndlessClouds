using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EndlessClouds.CheckDuplicates
{
    public class UserDatabase
    {
        public string Name { get; }
        public Dictionary<int, T_User> Users { get; set; }

        public UserDatabase(string name, List<T_User> users)
        {
            Name = name;
            Users = users?.ToDictionary(k => k.UsernameHash, v => v) ?? new Dictionary<int, T_User>();
        }

        public bool TryCreateUser(string username, out T_User user)
        {
            user = default;

            if (!username.All(char.IsLetterOrDigit) || !(username.Length is >= 6 and <= 30))
                return false;

            user = new T_User(Guid.NewGuid(), username, GenerateHash(username));

            /* Hash  */
            if (Users.ContainsKey(user.UsernameHash))
                return false; // user exists

            Users[user.UsernameHash] = user;

            return true;
        }

        public bool TryUpdateUser(Guid accountId, string oldUsername, string newUsername)
        {
            if (accountId == Guid.Empty || string.IsNullOrEmpty(newUsername) || !(newUsername.Length is >= 6 and <= 30)) return false;

            var userHash = GenerateHash(oldUsername);

            /* We could use Remove and return the Value removed but we would lose our oneliner accountId check */
            if (Users.TryGetValue(userHash, out var user))
            {
                if (user.AccountId != accountId) return false; // non matching accountIds? :thinking:

                Users.Remove(userHash); // remove the old record

                user.Username = newUsername;
                user.UsernameHash = GenerateHash(newUsername);
                user.UpdatedAt = DateTime.Now;

                Users[user.UsernameHash] = user; // re-add the record
            }

            return true; // we could use Users.ContainsKey(userHash) but it would add more time to the call
        }


        public string GenerateSQLTable()
        {
            var tableCreation = new StringBuilder();

            tableCreation.AppendLine($@"CREATE TABLE {Name} (
                                           Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
                                           AccountId varchar(64) NOT NULL,
                                           Username varchar(31) NOT NULL,
                                           UsernameHash int NOT NULL,
                                           CreatedAt DateTime,
                                           UpdatedAt DateTime);");




            return tableCreation.ToString();
        }

        public string[] GenerateSQLData()
        {
            // split data into strings of 10k
            var users = Users.Values.ToList();
            var strings = new List<string>();
            Stopwatch watch = new Stopwatch();
            var str = new StringBuilder();
            for (int i = 0; i < users.Count; i += 10000)
            {
                str.Clear();

                watch.Restart();
                var ranged = users.GetRange(i, Math.Min(10000, users.Count - i));
      
                for (int x = 0; x < ranged.Count; x++)
                {
                    var user = ranged[x];
                    str.AppendLine($"INSERT INTO {Name} VALUES (0,'{user.AccountId}', '{user.Username}', {user.UsernameHash}, '{user.CreatedAt:s}', '{user.UpdatedAt:s}');");
                }
                strings.Add(str.ToString());
                watch.Stop();
                Console.WriteLine($"({i}) Creating a list of 10K users took {watch.Elapsed.TotalMilliseconds}ms");
            }

            return strings.ToArray();
        }


        private static int GenerateHash(string str)
        {
            MD5 md5Creator = MD5.Create();
            var md5Hash = md5Creator.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToInt32(md5Hash, 0); // better find a faster alternative (Spans maybe?)
        }

    }

    public struct T_User
    {
        public Guid AccountId { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /* Helps with the search */
        public int UsernameHash { get; set; }

        public T_User(Guid accountId, string username, int hash) : this()
        {
            AccountId = accountId;
            Username = username;
            UsernameHash = hash;

            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
