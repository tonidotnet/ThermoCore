using ThermoCore.Core.Validation;

namespace ThermoCore.Core.Balances;

/// <summary>
/// Component conservation terms for one timestep
/// (docs/02_Mathematics/04_MathematicalModel.md §57).
/// Residual convention: R = inputs - outputs - accumulation.
/// </summary>
public sealed record ConservationBalance
{
    public double DryAirMassInputKg { get; init; }

    public double DryAirMassOutputKg { get; init; }

    public double DryAirMassStorageChangeKg { get; init; }

    public double DryAirMassResidualKg { get; init; }

    public double WaterMassInputKg { get; init; }

    public double WaterMassOutputKg { get; init; }

    public double WaterMassStorageChangeKg { get; init; }

    public double WaterMassResidualKg { get; init; }

    public double EnergyInputJ { get; init; }

    public double EnergyOutputJ { get; init; }

    public double StoredEnergyChangeJ { get; init; }

    public double EnergyResidualJ { get; init; }

    public double ElectricalEnergyInputJ { get; init; }

    public double ElectricalEnergyOutputJ { get; init; }

    public double StoredElectricalEnergyChangeJ { get; init; }

    public double ElectricalEnergyResidualJ { get; init; }

    public static ConservationBalance Empty { get; } = new();

    public static ConservationBalance FromTerms(
        double dryAirMassInputKg,
        double dryAirMassOutputKg,
        double dryAirMassStorageChangeKg,
        double waterMassInputKg,
        double waterMassOutputKg,
        double waterMassStorageChangeKg,
        double energyInputJ,
        double energyOutputJ,
        double storedEnergyChangeJ,
        double electricalEnergyInputJ = 0.0,
        double electricalEnergyOutputJ = 0.0,
        double storedElectricalEnergyChangeJ = 0.0)
    {
        RequireFinite(dryAirMassInputKg, nameof(dryAirMassInputKg));
        RequireFinite(dryAirMassOutputKg, nameof(dryAirMassOutputKg));
        RequireFinite(dryAirMassStorageChangeKg, nameof(dryAirMassStorageChangeKg));
        RequireFinite(waterMassInputKg, nameof(waterMassInputKg));
        RequireFinite(waterMassOutputKg, nameof(waterMassOutputKg));
        RequireFinite(waterMassStorageChangeKg, nameof(waterMassStorageChangeKg));
        RequireFinite(energyInputJ, nameof(energyInputJ));
        RequireFinite(energyOutputJ, nameof(energyOutputJ));
        RequireFinite(storedEnergyChangeJ, nameof(storedEnergyChangeJ));
        RequireFinite(electricalEnergyInputJ, nameof(electricalEnergyInputJ));
        RequireFinite(electricalEnergyOutputJ, nameof(electricalEnergyOutputJ));
        RequireFinite(storedElectricalEnergyChangeJ, nameof(storedElectricalEnergyChangeJ));

        return new ConservationBalance
        {
            DryAirMassInputKg = dryAirMassInputKg,
            DryAirMassOutputKg = dryAirMassOutputKg,
            DryAirMassStorageChangeKg = dryAirMassStorageChangeKg,
            DryAirMassResidualKg = dryAirMassInputKg - dryAirMassOutputKg - dryAirMassStorageChangeKg,
            WaterMassInputKg = waterMassInputKg,
            WaterMassOutputKg = waterMassOutputKg,
            WaterMassStorageChangeKg = waterMassStorageChangeKg,
            WaterMassResidualKg = waterMassInputKg - waterMassOutputKg - waterMassStorageChangeKg,
            EnergyInputJ = energyInputJ,
            EnergyOutputJ = energyOutputJ,
            StoredEnergyChangeJ = storedEnergyChangeJ,
            EnergyResidualJ = energyInputJ - energyOutputJ - storedEnergyChangeJ,
            ElectricalEnergyInputJ = electricalEnergyInputJ,
            ElectricalEnergyOutputJ = electricalEnergyOutputJ,
            StoredElectricalEnergyChangeJ = storedElectricalEnergyChangeJ,
            ElectricalEnergyResidualJ = electricalEnergyInputJ - electricalEnergyOutputJ - storedElectricalEnergyChangeJ
        };
    }

    public static ConservationBalance FromRates(
        double dryAirMassInputKgPerSecond,
        double dryAirMassOutputKgPerSecond,
        double dryAirMassStorageChangeKgPerSecond,
        double waterMassInputKgPerSecond,
        double waterMassOutputKgPerSecond,
        double waterMassStorageChangeKgPerSecond,
        double energyInputW,
        double energyOutputW,
        double storedEnergyChangeW,
        TimeSpan timeStep,
        double electricalPowerInputW = 0.0,
        double electricalPowerOutputW = 0.0,
        double storedElectricalPowerChangeW = 0.0)
    {
        FiniteNumber.RequirePositive(timeStep.TotalSeconds, nameof(timeStep));
        var dt = timeStep.TotalSeconds;

        return FromTerms(
            dryAirMassInputKgPerSecond * dt,
            dryAirMassOutputKgPerSecond * dt,
            dryAirMassStorageChangeKgPerSecond * dt,
            waterMassInputKgPerSecond * dt,
            waterMassOutputKgPerSecond * dt,
            waterMassStorageChangeKgPerSecond * dt,
            energyInputW * dt,
            energyOutputW * dt,
            storedEnergyChangeW * dt,
            electricalPowerInputW * dt,
            electricalPowerOutputW * dt,
            storedElectricalPowerChangeW * dt);
    }

    public ConservationBalance Aggregate(ConservationBalance other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return FromTerms(
            DryAirMassInputKg + other.DryAirMassInputKg,
            DryAirMassOutputKg + other.DryAirMassOutputKg,
            DryAirMassStorageChangeKg + other.DryAirMassStorageChangeKg,
            WaterMassInputKg + other.WaterMassInputKg,
            WaterMassOutputKg + other.WaterMassOutputKg,
            WaterMassStorageChangeKg + other.WaterMassStorageChangeKg,
            EnergyInputJ + other.EnergyInputJ,
            EnergyOutputJ + other.EnergyOutputJ,
            StoredEnergyChangeJ + other.StoredEnergyChangeJ,
            ElectricalEnergyInputJ + other.ElectricalEnergyInputJ,
            ElectricalEnergyOutputJ + other.ElectricalEnergyOutputJ,
            StoredElectricalEnergyChangeJ + other.StoredElectricalEnergyChangeJ);
    }

    private static void RequireFinite(double value, string name) => FiniteNumber.Require(value, name);
}
