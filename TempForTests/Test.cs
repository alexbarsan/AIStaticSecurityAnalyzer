using System.Security.Cryptography;

namespace Analyzer.Core;

public class Test
{
    public void Hash(string input)
    {
        var key = "1234";
        var password = "superMarioa123";
        var apiKey = "preProd_live_1234567890";
        var md5 = MD5.Create();
        var bytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("input"));
    }
}

public class Secrets
{
    public void Login()
    {
        var key = "1234";S
        var password = "superMarioa123";
        var apiKey = "preProd_live_1234567890";
    }
}