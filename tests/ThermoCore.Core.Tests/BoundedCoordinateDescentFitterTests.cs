using ThermoCore.Core.Calibration;

namespace ThermoCore.Core.Tests;

public class BoundedCoordinateDescentFitterTests
{
    [Fact]
    public void Fit_RecoversQuadraticMinimumInsideBounds()
    {
        var result = BoundedCoordinateDescentFitter.Fit(new ParameterFittingRequest
        {
            Parameters =
            [
                new CalibratableParameter
                {
                    Id = "x",
                    InitialValue = 0.0,
                    LowerBound = -2.0,
                    UpperBound = 2.0
                }
            ],
            MaximumPasses = 3,
            MaximumEvaluationsPerParameter = 24,
            RelativeTolerance = 1e-6,
            Objective = values =>
            {
                var x = values["x"];
                return (x - 0.7) * (x - 0.7);
            }
        });

        Assert.True(result.Improved);
        Assert.InRange(result.FittedValues["x"], 0.69, 0.71);
        Assert.True(result.FinalObjective < 1e-3);
    }
}
