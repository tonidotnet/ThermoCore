namespace ThermoCore.Core.Components;

public sealed class ComponentTemplate
{
    public string Name { get; init; } = string.Empty;

    public EvaluationResult Evaluate(EvaluationContext context)
    {
        throw new NotImplementedException();
    }

    public void Commit()
    {
    }
}
