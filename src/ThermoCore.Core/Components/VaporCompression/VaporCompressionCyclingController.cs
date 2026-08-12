using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Core.Components.VaporCompression;

/// <summary>
/// Deterministic compressor on/off gating with minimum runtime and off-time (COOL-007 / R5-002).
/// </summary>
public sealed class VaporCompressionCyclingController
{
    private readonly VaporCompressionCyclingLimits _limits;
    private bool _compressorOn;
    private TimeSpan _timeInState;

    public VaporCompressionCyclingController(VaporCompressionCyclingLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        _limits = limits.Validate();
        // Allow an immediate first start from a cold plant.
        _compressorOn = false;
        _timeInState = _limits.MinimumOffTime;
    }

    public bool IsOn => _compressorOn;

    public TimeSpan TimeInState => _timeInState;

    public void Reset()
    {
        _compressorOn = false;
        _timeInState = _limits.MinimumOffTime;
    }

    public VaporCompressionCyclingDecision Step(bool requestedOn, TimeSpan timeStep)
    {
        if (timeStep < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeStep));
        }

        var diagnostics = new List<SimulationDiagnostic>();
        var held = false;

        if (requestedOn == _compressorOn)
        {
            _timeInState += timeStep;
            return new VaporCompressionCyclingDecision(_compressorOn, held, diagnostics);
        }

        if (_compressorOn && !requestedOn)
        {
            if (_timeInState < _limits.MinimumRuntime)
            {
                held = true;
                _timeInState += timeStep;
                diagnostics.Add(HoldDiagnostic(
                    "VC.CYCLING_MIN_RUNTIME",
                    "Compressor remains on until the minimum runtime elapses.",
                    _limits.MinimumRuntime,
                    _timeInState));
                return new VaporCompressionCyclingDecision(true, held, diagnostics);
            }

            _compressorOn = false;
            _timeInState = timeStep;
            return new VaporCompressionCyclingDecision(false, held, diagnostics);
        }

        // requested on while currently off
        if (_timeInState < _limits.MinimumOffTime)
        {
            held = true;
            _timeInState += timeStep;
            diagnostics.Add(HoldDiagnostic(
                "VC.CYCLING_MIN_OFF_TIME",
                "Compressor remains off until the minimum off-time elapses.",
                _limits.MinimumOffTime,
                _timeInState));
            return new VaporCompressionCyclingDecision(false, held, diagnostics);
        }

        _compressorOn = true;
        _timeInState = timeStep;
        return new VaporCompressionCyclingDecision(true, held, diagnostics);
    }

    private static SimulationDiagnostic HoldDiagnostic(
        string code,
        string message,
        TimeSpan required,
        TimeSpan elapsed)
        => new()
        {
            Code = code,
            Severity = DiagnosticSeverity.Information,
            Message = message,
            Values = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["requiredSeconds"] = required.TotalSeconds,
                ["elapsedInStateSeconds"] = elapsed.TotalSeconds
            }
        };
}

/// <summary>One cycling step outcome.</summary>
public readonly record struct VaporCompressionCyclingDecision(
    bool CompressorOn,
    bool HeldByCyclingLimits,
    IReadOnlyList<SimulationDiagnostic> Diagnostics);
