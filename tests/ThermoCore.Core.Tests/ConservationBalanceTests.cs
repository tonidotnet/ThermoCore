using ThermoCore.Core.Balances;
using ThermoCore.Core.Diagnostics;

namespace ThermoCore.Core.Tests;

public class ConservationBalanceTests
{
    [Fact]
    public void FromTerms_PerfectlyBalanced_HasZeroResiduals()
    {
        var balance = ConservationBalance.FromTerms(
            dryAirMassInputKg: 1.0,
            dryAirMassOutputKg: 1.0,
            dryAirMassStorageChangeKg: 0.0,
            waterMassInputKg: 0.01,
            waterMassOutputKg: 0.01,
            waterMassStorageChangeKg: 0.0,
            energyInputJ: 1000.0,
            energyOutputJ: 1000.0,
            storedEnergyChangeJ: 0.0);

        Assert.Equal(0.0, balance.DryAirMassResidualKg);
        Assert.Equal(0.0, balance.WaterMassResidualKg);
        Assert.Equal(0.0, balance.EnergyResidualJ);
    }

    [Fact]
    public void FromTerms_WithStorage_UsesInputMinusOutputMinusAccumulation()
    {
        var balance = ConservationBalance.FromTerms(
            dryAirMassInputKg: 2.0,
            dryAirMassOutputKg: 1.5,
            dryAirMassStorageChangeKg: 0.5,
            waterMassInputKg: 0.0,
            waterMassOutputKg: 0.0,
            waterMassStorageChangeKg: 0.0,
            energyInputJ: 10.0,
            energyOutputJ: 4.0,
            storedEnergyChangeJ: 6.0);

        Assert.Equal(0.0, balance.DryAirMassResidualKg);
        Assert.Equal(0.0, balance.EnergyResidualJ);
    }

    [Fact]
    public void FromRates_IntegratesOverTimestep()
    {
        var balance = ConservationBalance.FromRates(
            dryAirMassInputKgPerSecond: 0.02,
            dryAirMassOutputKgPerSecond: 0.02,
            dryAirMassStorageChangeKgPerSecond: 0.0,
            waterMassInputKgPerSecond: 0.001,
            waterMassOutputKgPerSecond: 0.001,
            waterMassStorageChangeKgPerSecond: 0.0,
            energyInputW: 100.0,
            energyOutputW: 100.0,
            storedEnergyChangeW: 0.0,
            timeStep: TimeSpan.FromSeconds(2.0));

        Assert.Equal(0.04, balance.DryAirMassInputKg, precision: 12);
        Assert.Equal(200.0, balance.EnergyInputJ, precision: 12);
    }

    [Fact]
    public void Aggregate_SumsComponentBalances()
    {
        var a = ConservationBalance.FromTerms(1, 1, 0, 0.1, 0.1, 0, 10, 10, 0);
        var b = ConservationBalance.FromTerms(2, 2, 0, 0.2, 0.2, 0, 20, 20, 0);
        var system = a.Aggregate(b);

        Assert.Equal(3.0, system.DryAirMassInputKg);
        Assert.Equal(0.3, system.WaterMassInputKg, precision: 12);
        Assert.Equal(30.0, system.EnergyInputJ);
        Assert.Equal(0.0, system.EnergyResidualJ);
    }

    [Fact]
    public void Validate_UnbalancedEnergy_ReturnsErrorDiagnostic()
    {
        var balance = ConservationBalance.FromTerms(
            dryAirMassInputKg: 1.0,
            dryAirMassOutputKg: 1.0,
            dryAirMassStorageChangeKg: 0.0,
            waterMassInputKg: 0.0,
            waterMassOutputKg: 0.0,
            waterMassStorageChangeKg: 0.0,
            energyInputJ: 100.0,
            energyOutputJ: 50.0,
            storedEnergyChangeJ: 0.0);

        var result = new ConservationValidator().Validate(balance);

        Assert.False(result.IsValid);
        Assert.Contains(result.Diagnostics, d => d.Code == "BALANCE.ENERGY" && d.Severity == DiagnosticSeverity.Error);
        Assert.True(result.RelativeEnergyResidual > 0.0);
    }

    [Fact]
    public void Validate_BalancedWithinTolerance_IsValid()
    {
        var balance = ConservationBalance.FromTerms(
            dryAirMassInputKg: 1.0,
            dryAirMassOutputKg: 1.0,
            dryAirMassStorageChangeKg: 0.0,
            waterMassInputKg: 0.01,
            waterMassOutputKg: 0.01,
            waterMassStorageChangeKg: 0.0,
            energyInputJ: 1_000.0,
            energyOutputJ: 1_000.0 + 1e-8,
            storedEnergyChangeJ: 0.0);

        var result = new ConservationValidator().Validate(balance, BalanceTolerance.Default);
        Assert.True(result.IsValid);
    }
}
