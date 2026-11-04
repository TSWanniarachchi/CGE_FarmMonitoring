/*
 * Description  : Defines the success / failure result returned by Application services.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Common.Results;

/// <summary>
/// Outcome of an Application service operation that returns no value.
/// Services return a failed result for expected outcomes (validation, not found, conflict, forbidden)
/// instead of throwing; exceptions are reserved for unexpected failures.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess && !ReferenceEquals(error, Error.None))
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));

        if (!isSuccess && ReferenceEquals(error, Error.None))
            throw new ArgumentException("A failed result must carry an error.", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    #region Properties

    /// <summary>
    /// True when the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// True when the operation failed; <see cref="Error"/> describes why.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The error of a failed result, or <see cref="Error.None"/> when successful.
    /// </summary>
    public Error Error { get; }

    #endregion

    #region Factories

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    #endregion

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// Outcome of an Application service operation that returns a value on success.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// The value of a successful result. Throws when the result failed; check <see cref="Result.IsSuccess"/> first.
    /// </summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error}).");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
