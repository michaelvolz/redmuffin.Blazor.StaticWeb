public class AbstractionTarget
{
    public string Format(string value, string mode)
    {
        return ApplyMode(value, mode);
    }

    private string ApplyMode(string input, string mode)
    {
        if (mode == "upper")
            return input.ToUpper();
        return input;
    }
}
// Expected: ApplyMode → wrong-abstraction (if on formal param "mode") → composite=2 WARN
