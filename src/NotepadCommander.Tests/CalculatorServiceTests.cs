using Xunit;
using NotepadCommander.Core.Services.Calculator;

namespace NotepadCommander.Tests;

public class CalculatorServiceTests
{
    private readonly CalculatorService _service = new();

    [Fact]
    public void Evaluate_SimpleAddition()
    {
        var result = _service.Evaluate("2 + 3");
        Assert.True(result.Success);
        Assert.Equal("5", result.Result);
    }

    [Fact]
    public void Evaluate_Multiplication()
    {
        var result = _service.Evaluate("6 * 7");
        Assert.True(result.Success);
        Assert.Equal("42", result.Result);
    }

    [Fact]
    public void Evaluate_Division()
    {
        var result = _service.Evaluate("10 / 2");
        Assert.True(result.Success);
        Assert.Equal("5", result.Result);
    }

    [Fact]
    public void Evaluate_ComplexExpression()
    {
        var result = _service.Evaluate("(2 + 3) * 4");
        Assert.True(result.Success);
        Assert.Equal("20", result.Result);
    }

    [Fact]
    public void Evaluate_EmptyExpression_Fails()
    {
        var result = _service.Evaluate("");
        Assert.False(result.Success);
    }

    [Fact]
    public void Evaluate_InvalidExpression_Fails()
    {
        var result = _service.Evaluate("abc xyz");
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Evaluate_Modulus()
    {
        var result = _service.Evaluate("10 % 3");
        Assert.True(result.Success);
        Assert.Equal("1", result.Result);
    }

    [Fact]
    public void Evaluate_Power()
    {
        var result = _service.Evaluate("Pow(2, 8)");
        Assert.True(result.Success);
        Assert.Equal("256", result.Result);
    }
}
