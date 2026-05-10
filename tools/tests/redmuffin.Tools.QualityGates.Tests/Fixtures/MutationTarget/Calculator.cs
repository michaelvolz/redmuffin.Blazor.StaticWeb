namespace MutationTarget;

public static class Calculator
{
    public static int Add(int a, int b) => a + b;
    public static int Multiply(int a, int b) => a * b;
    public static bool IsPositive(int x) => x > 0;
    public static bool IsEqual(int a, int b) => a == b;
    public static bool IsTrue() => true;
    public static bool IsEven(int x) => x % 2 == 0;
    public static int GetZero() => 0;
}
