public class DeepTarget
{
    public int Compute(int x, int y)
    {
        if (x > 0)
        {
            var temp = Process(x);
            return temp + y;
        }
        return y;
    }

    private int Process(int value)
    {
        var result = 0;
        for (var i = 0; i < value; i++)
        {
            if (i % 2 == 0)
                result += i;
        }
        return result;
    }

    public DeepTarget(int a, int b, int c, int d, int e)
    {
        // constructor — public, not flagged as shallow
    }
}
// Expected: Compute → CLEAN (LOC>4, branching, public) → composite=0
// Expected: Process → CLEAN (LOC>4, loops+branching) → composite=0
// Expected: DeepTarget constructor → NOT shallow (public constructor)
