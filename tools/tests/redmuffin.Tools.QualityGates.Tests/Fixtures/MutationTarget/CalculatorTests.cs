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

    [Test]
    public async Task Multiply_should_return_product()
    {
        var result = Calculator.Multiply(3, 4);
        await Assert.That(result).IsEqualTo(12);
    }

    [Test]
    public async Task IsPositive_should_return_true_for_positive()
    {
        var result = Calculator.IsPositive(5);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsPositive_should_return_true_for_one()
    {
        var result = Calculator.IsPositive(1);
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsPositive_should_return_false_for_zero()
    {
        var result = Calculator.IsPositive(0);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsPositive_should_return_false_for_negative()
    {
        var result = Calculator.IsPositive(-5);
        await Assert.That(result).IsFalse();
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
