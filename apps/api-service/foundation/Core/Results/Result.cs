using System;
using Aurore.Foundation.Core.Errors;

namespace Aurore.Foundation.Core.Results;

/// <summary>
/// The outcome of an operation that has no value: it succeeded, or failed with an <see cref="ApplicationError"/>.
/// Build one without writing a factory: <c>return AppErrors.NotFound;</c> converts to a failed result, and
/// <c>return Result.Success;</c> is the success.
/// </summary>
public record Result
{
    /// <summary>Creates a result. Use Success, Failure or the implicit conversions instead.</summary>
    protected Result(ApplicationError? error)
    {
        Error = error;
    }

    /// <summary>The successful result of an operation that has no value.</summary>
    public static Result Success { get; } = new(error: null);

    /// <summary>
    /// The failure, or null on success.
    /// </summary>
    public ApplicationError? Error { get; }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => Error is not null;

    /// <summary>Creates a failed result.</summary>
    public static Result Failure(ApplicationError error)
    {
        return new(error);
    }

    /// <summary>Lets a method return an error directly.</summary>
    public static implicit operator Result(ApplicationError error)
    {
        return Failure(error);
    }
}

/// <summary>
/// The outcome of an operation that produces a <typeparamref name="T"/> on success, or fails with an <see cref="ApplicationError"/>.
/// <c>return value;</c> and <c>return AppErrors.NotFound;</c> both convert implicitly.
/// </summary>
public sealed record Result<T> : Result
{
    private Result(T value)
        : base(error: null)
    {
        Value = value;
    }

    private Result(ApplicationError error)
        : base(error)
    {
        Value = default!;
    }

    /// <summary>
    /// The value of a successful result. Reading it from a failed result is a programming mistake and throws.
    /// </summary>
    public T Value
    {
        get => IsSuccess
            ? field
            : throw new InvalidOperationException($"The result failed ({Error!.Code}), so it has no value.");
        private init;
    }

    /// <summary>Creates a successful result carrying the value.</summary>
    public static new Result<T> Success(T value)
    {
        return new(value);
    }

    /// <summary>Creates a failed result.</summary>
    public static new Result<T> Failure(ApplicationError error)
    {
        return new(error);
    }

    /// <summary>Lets a method return a value directly.</summary>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }

    /// <summary>Lets a method return an error directly.</summary>
    public static implicit operator Result<T>(ApplicationError error)
    {
        return Failure(error);
    }
}
