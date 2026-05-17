public class BloatTarget
{
    public void Configure(string a, string b, int c, bool d, double e)
    {
        Console.WriteLine($"{a} {b} {c} {d} {e}");
    }
}
// Expected: Configure → param-bloat (5 params > 4) → composite=1 INFO
// Expected: Configure → NOT shallow (public)
