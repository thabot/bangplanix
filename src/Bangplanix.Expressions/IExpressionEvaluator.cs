namespace Bangplanix.Expressions;

public interface IExpressionEvaluator
{
    TResult? Evaluate<TResult>(string expression, object? context = null);
    object? Evaluate(string expression, object? context = null);
    bool ValidateExpressionSafety(string expression, out string? securityViolationReason);
}
