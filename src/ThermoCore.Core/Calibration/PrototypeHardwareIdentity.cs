namespace ThermoCore.Core.Calibration;

/// <summary>Hardware identity preserved with a prototype campaign (VAL-002).</summary>
public sealed record PrototypeHardwareIdentity
{
    public required string Manufacturer { get; init; }

    public required string Model { get; init; }

    public string? SerialNumber { get; init; }

    public string? FirmwareVersion { get; init; }

    /// <summary>e.g. commercial-peltier-dehumidifier, bare-tec, awg-v3-prototype.</summary>
    public required string HardwareClass { get; init; }

    public string? Notes { get; init; }

    public PrototypeHardwareIdentity Validate()
    {
        RequireNonEmpty(Manufacturer, nameof(Manufacturer));
        RequireNonEmpty(Model, nameof(Model));
        RequireNonEmpty(HardwareClass, nameof(HardwareClass));
        return this;
    }

    private static void RequireNonEmpty(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} is required.", name);
        }
    }
}

/// <summary>Reference to a sensor calibration certificate / ID used in the campaign.</summary>
public sealed record PrototypeSensorCalibrationRef
{
    /// <summary>Role such as inlet-temperature, inlet-rh, power-meter.</summary>
    public required string Role { get; init; }

    public required string CalibrationId { get; init; }

    public string? Quantity { get; init; }

    public string? Unit { get; init; }

    public DateOnly? CalibrationDate { get; init; }

    public string? Notes { get; init; }

    public PrototypeSensorCalibrationRef Validate()
    {
        if (string.IsNullOrWhiteSpace(Role))
        {
            throw new ArgumentException("Role is required.", nameof(Role));
        }

        if (string.IsNullOrWhiteSpace(CalibrationId))
        {
            throw new ArgumentException("CalibrationId is required.", nameof(CalibrationId));
        }

        return this;
    }
}
