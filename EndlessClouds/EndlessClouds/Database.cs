using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EndlessClouds
{
    /// <summary>
    /// Please keep in mind this is not a REAL Database. 
    /// </summary>
    public class Database
    {
        public string Name { get; }

        public List<T_Token> TokensGenerated { get; }

        public Database(string name, List<T_Token> tokensGenerated)
        {
            Name = name;
            TokensGenerated = tokensGenerated ?? new List<T_Token>(100);
        }

        public void AddToken(T_Token token) => TokensGenerated.Add(token);
        public bool RemoveToken(string token) => TokensGenerated.Remove(TokensGenerated.Find(i => i.Token == token));
        public T_Token GetToken(Guid accountId)
        {
            var latestToken = TokensGenerated.LastOrDefault(i => i.AccountId.Equals(accountId) && !i.TokenExpired);
            return string.IsNullOrEmpty(latestToken.Token) ? GenerateToken(accountId) : latestToken;
        }

        public List<T_Token> GetTokens(Guid accountId, int count) => TokensGenerated.Where(i => i.AccountId.Equals(accountId)).Take(count).ToList();

        public T_Token GenerateToken(Guid accountId)
        {
            /* https://stackoverflow.com/a/730418 */
            Stopwatch watch = new Stopwatch();
            watch.Start();
            Guid g = Guid.NewGuid();
            string guidString = Convert.ToBase64String(g.ToByteArray());
            guidString = guidString.Replace("=", "");
            guidString = guidString.Replace("+", "");

            // insert date in the middle of the token
            guidString = guidString.Insert(guidString.Length / 2, $"{DateTime.Now:yyyy#MM#dd$HH/mm/ss$}");

            var token = new T_Token(accountId, guidString);
            AddToken(token);

            watch.Stop();

            //Console.WriteLine($"Generating a new Token for ({accountId}) Took {watch.Elapsed.TotalMilliseconds}ms");
            return token;
        }

        public string GenerateSQLTable()
        {
            var tableCreation = new StringBuilder();

            tableCreation.AppendLine($@"CREATE TABLE {Name} (
                                           Id int NOT NULL AUTO_INCREMENT PRIMARY KEY,
                                           AccountId varchar(64) NOT NULL,
                                           Token varchar(50) NOT NULL,
                                           CreatedAt DateTime);");
            return tableCreation.ToString();
        }

        public string[] GenerateSQLData()
        {
            // split data into strings of 10k
            var tokens = TokensGenerated;
            var strings = new List<string>();
            Stopwatch watch = new Stopwatch();
            var str = new StringBuilder();
            for (int i = 0; i < tokens.Count; i += 10000)
            {
                str.Clear();

                watch.Restart();
                var ranged = tokens.GetRange(i, Math.Min(10000, tokens.Count - i));

                for (int x = 0; x < ranged.Count; x++)
                {
                    var user = ranged[x];
                    str.AppendLine($"INSERT INTO {Name} VALUES (0,'{user.AccountId}', '{user.Token}', '{user.CreatedAt:s}');");
                }
                strings.Add(str.ToString());
                watch.Stop();
                Console.WriteLine($"({i}) Creating a list of 10K Tokens took {watch.Elapsed.TotalMilliseconds}ms");
            }

            return strings.ToArray();
        }

    }

    /// <summary>
    /// T for Table
    /// </summary>
    public struct T_Token
    {
        public Guid AccountId; // GUID
        public string Token;
        public DateTime CreatedAt;
        public bool TokenExpired => (DateTime.Now - CreatedAt).TotalSeconds > 10;

        public T_Token(Guid accountId, string token)
        {
            AccountId = accountId;
            Token = token;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var date = Token.Substring((token.Length / 2) - (20 / 2), 20).Replace("#", "-").Replace("/", ":").Replace("$", " ");
            CreatedAt = DateTime.Parse(date);

            watch.Stop();

            //Console.WriteLine($"Parsing Token for ({accountId}) Took {watch.Elapsed.TotalMilliseconds}ms");
        }


        public override string ToString()
        {
            return $"{Token} ({AccountId}) -> {CreatedAt:G}";
        }
    }
}
