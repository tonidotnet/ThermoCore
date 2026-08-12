namespace ThermoCore.AWG.Cooling;

/// <summary>AWG-level cooling plant orchestration model (ADR-016).</summary>
public interface ICoolingPlantModel
{
    CoolingTechnology Technology { get; }

    CoolingPlantResult Evaluate(CoolingPlantRequest request);
}
