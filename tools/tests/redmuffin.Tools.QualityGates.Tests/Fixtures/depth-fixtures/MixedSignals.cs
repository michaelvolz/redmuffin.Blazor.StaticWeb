public class MixedTarget
{
    private int Combine(int a, int b, int c, int d, int e)
    {
        return a + b;
    }
}
// Expected: Combine → shallow (LOC=2, private, no branching) AND
// param-bloat (5 params > 4) → composite=4 FAIL [shallow(3) + params(1)]
