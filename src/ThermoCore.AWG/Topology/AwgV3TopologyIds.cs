namespace ThermoCore.AWG.Topology;

/// <summary>Stable component identifiers for the AWG V3 MVP topology.</summary>
public static class AwgV3TopologyIds
{
    public const string TopologyId = "awg-v3-mvp";

    public const string TopologyVersion = "1.0";

    public const string AmbientSource = "ambient-source";

    public const string ProcessFan = "process-fan";

    public const string PeltierHotSideHx = "peltier-hot-side-hx";

    public const string SolarCollector = "solar-collector";

    public const string SilicaGelBed = "silica-gel-bed";

    public const string Condenser = "condenser";

    public const string ExhaustSink = "exhaust-sink";

    public const string WaterTank = "water-tank";

    public const string SolarRadiation = "solar-radiation";

    public const string PvPanel = "pv-panel";

    public const string PvSolarRadiation = "pv-solar-radiation";

    public const string PowerManager = "power-manager";

    public const string ElectricalBusSink = "electrical-bus-sink";

    public const string CurtailmentSink = "curtailment-sink";

    public const string FreshAirMixer = "fresh-air-mixer";

    public const string HeatRecovery = "heat-recovery";

    public const string RecirculationSplitter = "recirculation-splitter";

    public static class ModelIds
    {
        public const string PrescribedFlowFan = "prescribed-flow-fan";

        public const string DynamicLumpedCollector = "dynamic-lumped-collector";

        public const string MoistAirPassThrough = "moist-air-pass-through";

        public const string SilicaGelLdfLinear = "silica-gel-ldf-linear";

        public const string CondenserBypassFactor = "condenser-bypass-factor";

        public const string ConstantEfficiencyPv = "constant-efficiency-pv";

        public const string PowerManagerWithBattery = "power-manager-with-battery";
    }
}
