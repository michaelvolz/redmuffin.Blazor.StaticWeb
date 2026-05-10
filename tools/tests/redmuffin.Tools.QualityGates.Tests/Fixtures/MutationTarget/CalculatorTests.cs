using TUnit.Core;

namespace MutationTarget;

public sealed class CalculatorTests
{
    [Test]
    public async Task Add_should_return_sum()
    {
        var result = Calculator.Add(2, 3);
        await Assert.That(result).IsEqualTo(5);
    }

    // Intentionally NOT testing Multiply — so *→/ mutation survives

    [Test]
    public async Task IsPositive_should_return_true_for_positive()
    {
        var result = Calculator.IsPositive(5);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsEqual_should_return_true_for_equal()
    {
        var result = Calculator.IsEqual(3, 3);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsTrue_should_return_true()
    {
        var result = Calculator.IsTrue();
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsEven_should_return_true_for_even()
    {
        var result = Calculator.IsEven(4);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task GetZero_should_return_zero()
    {
        var result = Calculator.GetZero();
        await Assert.That(result).IsEqualTo(0);
    }
}
