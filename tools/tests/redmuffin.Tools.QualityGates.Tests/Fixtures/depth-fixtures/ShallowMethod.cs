public class ShallowTarget
{
    public void Caller()
    {
        var result = ShallowHelper(5);
        Console.WriteLine(result);
    }

    private int ShallowHelper(int x)
    {
        return x + 1;
    }
}
// Expected: ShallowHelper → shallow (LOC=1, private, no branching) → composite=3 FAIL
