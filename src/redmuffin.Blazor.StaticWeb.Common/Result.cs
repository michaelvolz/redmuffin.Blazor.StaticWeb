namespace redmuffin.Blazor.StaticWeb.Common;

/// <summary>
///     Discriminated success/failure for expected outcomes at module boundaries.
///     Cancellation and programmer bugs remain exceptions.
/// </summary>
public readonly struct Result<T> : IEquatable<Result<T>>
{
    private readonly T? _value;
    private readonly string? _error;

    internal Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public string Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);

    public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }

    public bool Equals(Result<T> other)
    {
        if (IsSuccess != other.IsSuccess)
            return false;

        return IsSuccess
            ? EqualityComparer<T?>.Default.Equals(_value, other._value)
            : string.Equals(_error, other._error, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is Result<T> other && Equals(other);

    public override int GetHashCode()
    {
        return IsSuccess
            ? HashCode.Combine(true, _value)
            : HashCode.Combine(false, _error);
    }
}

/// <summary>
///     Non-generic factories for <see cref="Result{T}"/> (avoids static members on the generic type).
/// </summary>
public static class Result
{
    public static Result<T> Success<T>(T value) => new(isSuccess: true, value, error: null);

    public static Result<T> Failure<T>(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        return new Result<T>(isSuccess: false, value: default, error);
    }
}
