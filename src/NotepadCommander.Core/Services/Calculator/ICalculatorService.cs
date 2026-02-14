namespace NotepadCommander.Core.Services.Calculator;

public interface ICalculatorService
{
    CalculationResult Evaluate(string expression);
}

public class CalculationResult
{
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Error { get; set; }
}
