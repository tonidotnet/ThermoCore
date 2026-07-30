namespace ThermoCore.Core.Calibration;

/// <summary>
/// Bounded coordinate-descent fitter using golden-section search on each scalar parameter
/// (CAL-006; docs/02_Mathematics/25_NumericalMethods.md bounded search).
/// </summary>
public static class BoundedCoordinateDescentFitter
{
    private const double Phi = 1.6180339887498948482;

    public static ParameterFittingResult Fit(ParameterFittingRequest request)
    {
        request.Validate();

        var values = request.Parameters.ToDictionary(
            p => p.Id,
            p => p.InitialValue,
            StringComparer.Ordinal);
        var initial = new Dictionary<string, double>(values, StringComparer.Ordinal);
        var evaluations = 0;
        double Evaluate()
        {
            evaluations++;
            var objective = request.Objective(values);
            if (double.IsNaN(objective) || double.IsInfinity(objective))
            {
                return double.PositiveInfinity;
            }

            return objective;
        }

        var initialObjective = Evaluate();
        var currentObjective = initialObjective;
        var passes = 0;

        for (var pass = 0; pass < request.MaximumPasses; pass++)
        {
            passes++;
            var passStart = currentObjective;
            foreach (var parameter in request.Parameters)
            {
                currentObjective = OptimizeOne(parameter, values, Evaluate, request.MaximumEvaluationsPerParameter, currentObjective);
            }

            var scale = Math.Max(1.0, Math.Abs(passStart));
            if (Math.Abs(passStart - currentObjective) / scale <= request.RelativeTolerance)
            {
                break;
            }
        }

        return new ParameterFittingResult
        {
            InitialValues = initial,
            FittedValues = new Dictionary<string, double>(values, StringComparer.Ordinal),
            InitialObjective = initialObjective,
            FinalObjective = currentObjective,
            EvaluationCount = evaluations,
            PassCount = passes
        };
    }

    private static double OptimizeOne(
        CalibratableParameter parameter,
        Dictionary<string, double> values,
        Func<double> evaluate,
        int maximumEvaluations,
        double currentObjective)
    {
        var a = parameter.LowerBound;
        var b = parameter.UpperBound;
        if (Math.Abs(b - a) < 1e-15)
        {
            values[parameter.Id] = a;
            return currentObjective;
        }

        // Seed with current value evaluation already reflected in currentObjective.
        var bestValue = values[parameter.Id];
        var bestObjective = currentObjective;

        var c = b - (b - a) / Phi;
        var d = a + (b - a) / Phi;
        values[parameter.Id] = c;
        var fc = evaluate();
        values[parameter.Id] = d;
        var fd = evaluate();
        var used = 2;

        while (used < maximumEvaluations && Math.Abs(b - a) > 1e-9 * Math.Max(1.0, Math.Abs(bestValue)))
        {
            if (fc < fd)
            {
                b = d;
                d = c;
                fd = fc;
                c = b - (b - a) / Phi;
                values[parameter.Id] = c;
                fc = evaluate();
            }
            else
            {
                a = c;
                c = d;
                fc = fd;
                d = a + (b - a) / Phi;
                values[parameter.Id] = d;
                fd = evaluate();
            }

            used++;
            if (fc < bestObjective)
            {
                bestObjective = fc;
                bestValue = c;
            }

            if (fd < bestObjective)
            {
                bestObjective = fd;
                bestValue = d;
            }
        }

        values[parameter.Id] = bestValue;
        return bestObjective;
    }
}
