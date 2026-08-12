using ThermoCore.Core.Calibration;
using ThermoCore.Core.Units;
using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Components.Thermoelectric;

/// <summary>
/// Builds a commercial Peltier black-box profile from prototype measurement packages
/// using the existing calibration data path (R3-001 → R3-002).
/// </summary>
public static class CommercialPeltierDehumidifierProfileFitter
{
    public static CommercialPeltierDehumidifierProfile FromPackage(
        PrototypeMeasurementPackage package,
        string? profileId = null)
    {
        ArgumentNullException.ThrowIfNull(package);
        package.Campaign.Validate();
        if (package.Rows.Count < 2)
        {
            throw new ArgumentException(
                "At least two wide-CSV rows are required to derive water production rates from cumulative mass.",
                nameof(package));
        }

        var points = new List<CommercialPeltierMapPoint>();
        for (var i = 1; i < package.Rows.Count; i++)
        {
            var previous = package.Rows[i - 1];
            var current = package.Rows[i];
            if (current.InletTemperatureC is not { } tinC
                || current.InletRhPercent is not { } rhPercent
                || current.WaterMassG is not { } massG
                || previous.WaterMassG is not { } prevMassG)
            {
                continue;
            }

            var dt = (current.TimestampUtc - previous.TimestampUtc).TotalSeconds;
            if (dt <= 0.0)
            {
                continue;
            }

            var waterRate = Math.Max(0.0, (massG - prevMassG) * 1e-3 / dt);
            var power = ResolvePowerW(current);
            if (power is null)
            {
                continue;
            }

            points.Add(new CommercialPeltierMapPoint
            {
                InletTemperatureK = UnitConversions.CelsiusToKelvin(tinC),
                InletRelativeHumidityFraction = rhPercent / 100.0,
                ElectricalPowerW = power.Value,
                WaterProductionRateKgPerSecond = waterRate,
                DryAirMassFlowKgPerSecond = null,
                OutletTemperatureK = current.OutletTemperatureC is { } tout
                    ? UnitConversions.CelsiusToKelvin(tout)
                    : null,
                OutletRelativeHumidityFraction = current.OutletRhPercent is { } orh
                    ? orh / 100.0
                    : null,
                ColdSurfaceTemperatureK = current.ColdSurfaceTemperatureC is { } tc
                    ? UnitConversions.CelsiusToKelvin(tc)
                    : null,
                HotSideTemperatureK = current.HotSideTemperatureC is { } th
                    ? UnitConversions.CelsiusToKelvin(th)
                    : null
            }.Validate());
        }

        if (points.Count == 0)
        {
            throw new ArgumentException(
                "No usable map points could be derived (need inlet T/RH, water mass, and power/V·I).");
        }

        var supportsOutlet = points.All(p =>
            p.OutletTemperatureK is not null && p.OutletRelativeHumidityFraction is not null);
        var supportsFlow = points.Any(p => p.DryAirMassFlowKgPerSecond is not null);
        var hardware = package.Campaign.Hardware;

        return new CommercialPeltierDehumidifierProfile
        {
            ProfileId = profileId ?? $"commercial-peltier:{package.Campaign.CampaignId}",
            Manufacturer = hardware.Manufacturer,
            Model = hardware.Model,
            HardwareClass = hardware.HardwareClass,
            SourceIdentifier = package.Campaign.SourceIdentifier
                ?? package.CsvSourcePath,
            SourceRevision = package.Campaign.SourceRevision ?? "unknown",
            EvidenceLevel = MapEvidence(package.Campaign.ValidationLevel),
            ValidationLevel = package.Campaign.ValidationLevel,
            CampaignId = package.Campaign.CampaignId,
            Validity = CommercialPeltierValidityRange.FromPoints(points),
            MapPoints = points,
            SupportsOutletState = supportsOutlet,
            SupportsAirflowAxis = supportsFlow,
            FittingMethod =
                "Finite-difference water rate from cumulative waterMassG; inverse-distance map interpolation; validity = measured AABB plus small sensor/round-trip envelope.",
            Notes = package.Campaign.Notes
        }.Validate();
    }

    public static CommercialPeltierDehumidifierProfile FromMapPoints(
        IReadOnlyList<CommercialPeltierMapPoint> points,
        string profileId,
        string manufacturer,
        string model,
        TecEvidenceLevel evidenceLevel,
        string sourceIdentifier,
        string sourceRevision)
    {
        ArgumentNullException.ThrowIfNull(points);
        var validated = points.Select(p => p.Validate()).ToArray();
        if (validated.Length == 0)
        {
            throw new ArgumentException("Map points are required.", nameof(points));
        }

        return new CommercialPeltierDehumidifierProfile
        {
            ProfileId = profileId,
            Manufacturer = manufacturer,
            Model = model,
            SourceIdentifier = sourceIdentifier,
            SourceRevision = sourceRevision,
            EvidenceLevel = evidenceLevel,
            Validity = CommercialPeltierValidityRange.FromPoints(validated),
            MapPoints = validated,
            SupportsOutletState = validated.All(p =>
                p.OutletTemperatureK is not null && p.OutletRelativeHumidityFraction is not null),
            SupportsAirflowAxis = validated.Any(p => p.DryAirMassFlowKgPerSecond is not null),
            FittingMethod = "Explicit map points."
        }.Validate();
    }

    private static double? ResolvePowerW(PrototypeWideMeasurementRow row)
    {
        if (row.PowerW is { } power)
        {
            FiniteNumber.RequireNonNegative(power, nameof(row.PowerW));
            return power;
        }

        if (row.VoltageV is { } v && row.CurrentA is { } i)
        {
            var p = v * i;
            FiniteNumber.RequireNonNegative(p, "voltage*current");
            return p;
        }

        return null;
    }

    private static TecEvidenceLevel MapEvidence(PrototypeValidationLevel level)
        => level switch
        {
            PrototypeValidationLevel.OutdoorValidated => TecEvidenceLevel.Calibrated,
            PrototypeValidationLevel.IntegratedValidated => TecEvidenceLevel.MeasuredPrototype,
            _ => TecEvidenceLevel.MeasuredPrototype
        };
}
