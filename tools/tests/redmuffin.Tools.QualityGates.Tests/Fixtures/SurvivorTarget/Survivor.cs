namespace SurvivorTarget;

public static class Survivor
{
    public static int Add(int a, int b) => a + b;       // tested, should be killed
    public static int Multiply(int a, int b) => a * b;   // UNTESTED, should survive
}
