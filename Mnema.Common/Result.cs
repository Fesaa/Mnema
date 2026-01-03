using System;

namespace Mnema.Common;

public readonly struct Result<TValue, TException>
where TException: Exception
{
    private TValue? Value { get; init; }
    public TException? Error { get; init; }

    public bool IsOk => Error == null;
    public bool IsErr => Error != null;

    private Result(TValue value, TException error)
    {
        Value = value;
        Error = error;
    }

    public static Result<TValue, TException> Ok(TValue value) => new (value, null!);
    public static Result<TValue, TException> Err(TException error) => new (default!, error);

    public TResult Match<TResult>(Func<TValue, TResult> ok, Func<TException, TResult> err)
        => IsOk ? ok(Value!) : err(Error!);

    public void Match(Action<TValue> ok, Action<TException> err)
    {
        if (IsOk) ok(Value!);
        else err(Error!);
    }

    public TValue Unwrap() => IsOk ? Value! : throw new InvalidOperationException($"Called Unwrap on an Err value: {Error}");
    public TValue UnwrapOr(TValue defaultValue) => IsOk ? Value! : defaultValue;
    public TValue UnwrapOrElse(Func<TException, TValue> fn) => IsOk ? Value! : fn(Error!);

    public Result<TOtherValue, TException> Map<TOtherValue>(Func<TValue, TOtherValue> fn)
        => IsOk ? Result<TOtherValue, TException>.Ok(fn(Value!)) : Result<TOtherValue, TException>.Err(Error!);

    public Result<TValue, TOtherException> MapErr<TOtherException>(Func<TException, TOtherException> fn)
    where TOtherException: Exception
        => IsOk ? Result<TValue, TOtherException>.Ok(Value!) : Result<TValue, TOtherException>.Err(fn(Error!));
}