using NCalc;

namespace NotepadCommander.Core.Services.Calculator;

public class CalculatorService : ICalculatorService
{
    public CalculationResult Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return new CalculationResult { Success = false, Error = "Expression vide" };

        try
        {
            var expr = new Expression(expression);
            var result = expr.Evaluate();
            return new CalculationResult
            {
                Success = true,
                Result = result?.ToString() ?? "null"
            };
        }
        catch (Exception ex)
        {
            return new CalculationResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}
