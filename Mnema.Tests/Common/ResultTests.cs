using System;
using Mnema.Common;

namespace Mnema.Tests.Common;

public class ResultTests
{
    private class TestException(string message) : Exception(message);

    [Fact]
    public void Ok_CreatesSuccessResult()
    {
        var result = Result<int, TestException>.Ok(42);

        Assert.True(result.IsOk);
        Assert.False(result.IsErr);
        Assert.Equal(42, result.Unwrap());
    }

    [Fact]
    public void Err_CreatesErrorResult()
    {
        var error = new TestException("test error");
        var result = Result<int, TestException>.Err(error);

        Assert.False(result.IsOk);
        Assert.True(result.IsErr);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Unwrap_OnErr_ThrowsInvalidOperationException()
    {
        var result = Result<int, TestException>.Err(new TestException("test"));

        Assert.Throws<InvalidOperationException>(() => result.Unwrap());
    }

    [Fact]
    public void Match_WithFunc_ReturnsCorrectBranch()
    {
        var okResult = Result<int, TestException>.Ok(42);
        var errResult = Result<int, TestException>.Err(new TestException("fail"));

        Assert.Equal("Value: 42", okResult.Match(v => $"Value: {v}", e => $"Error: {e.Message}"));
        Assert.Equal("Error: fail", errResult.Match(v => $"Value: {v}", e => $"Error: {e.Message}"));
    }

    [Fact]
    public void Match_WithAction_ExecutesCorrectBranch()
    {
        var okResult = Result<int, TestException>.Ok(42);
        var errResult = Result<int, TestException>.Err(new TestException("fail"));
        string output = "";

        okResult.Match(v => output = $"ok:{v}", e => output = $"err:{e.Message}");
        Assert.Equal("ok:42", output);

        errResult.Match(v => output = $"ok:{v}", e => output = $"err:{e.Message}");
        Assert.Equal("err:fail", output);
    }

    [Fact]
    public void UnwrapOr_ReturnsValueOrDefault()
    {
        var okResult = Result<int, TestException>.Ok(42);
        var errResult = Result<int, TestException>.Err(new TestException("fail"));

        Assert.Equal(42, okResult.UnwrapOr(100));
        Assert.Equal(100, errResult.UnwrapOr(100));
    }

    [Fact]
    public void UnwrapOrElse_ReturnsValueOrComputedDefault()
    {
        var okResult = Result<int, TestException>.Ok(42);
        var errResult = Result<int, TestException>.Err(new TestException("fail"));

        Assert.Equal(42, okResult.UnwrapOrElse(_ => 999));
        Assert.Equal(999, errResult.UnwrapOrElse(_ => 999));
    }

    [Fact]
    public void Map_TransformsOkValue()
    {
        var result = Result<int, TestException>.Ok(42);
        var mapped = result.Map(x => x.ToString());

        Assert.True(mapped.IsOk);
        Assert.Equal("42", mapped.Unwrap());
    }

    [Fact]
    public void Map_PreservesError()
    {
        var error = new TestException("test");
        var result = Result<int, TestException>.Err(error);
        var mapped = result.Map(x => x.ToString());

        Assert.True(mapped.IsErr);
        Assert.Same(error, mapped.Error);
    }

    [Fact]
    public void MapErr_TransformsError()
    {
        var result = Result<int, TestException>.Err(new TestException("original"));
        var mapped = result.MapErr(e => new ArgumentException(e.Message + " mapped"));

        Assert.True(mapped.IsErr);
        Assert.IsType<ArgumentException>(mapped.Error);
        Assert.Equal("original mapped", mapped.Error.Message);
    }

    [Fact]
    public void MapErr_PreservesOkValue()
    {
        var result = Result<int, TestException>.Ok(42);
        var mapped = result.MapErr(e => new ArgumentException(e.Message));

        Assert.True(mapped.IsOk);
        Assert.Equal(42, mapped.Unwrap());
    }
}