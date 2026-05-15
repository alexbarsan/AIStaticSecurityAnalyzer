using System.Security.Cryptography;

public static class WeakHashingFixture
{
    public static void Run()
    {
        var sha256 = SHA256.Create();
    }
}
