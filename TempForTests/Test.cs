using System.Security.Cryptography;

namespace Analyzer.Core;

public class Test
{
    public void Hash(string input)
    {
        var md5 = MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
    }
}

public class Secrets
{
    public void Login()
    {
        var password = "superMarioa123";
        var apiKey = "preProd_live_1234567890";
    }
}