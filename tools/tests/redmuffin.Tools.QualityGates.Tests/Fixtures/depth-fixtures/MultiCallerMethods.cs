public class MultiCallerTarget
{
    public int FirstCaller()
    {
        return SharedHelper(10);
    }

    public int SecondCaller()
    {
        return SharedHelper(20);
    }

    public string ThirdCaller()
    {
        return SharedHelper(30).ToString();
    }

    private int SharedHelper(int x)
    {
        return x + 1;
    }
}
// Expected: SharedHelper → shallow by Phase 1 rules (private, LOC=1, no branching)
// BUT Phase 2 suppresses shallow because called from 3+ distinct methods
// → composite drops from 3 to 0 → excluded from results
