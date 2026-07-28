namespace ThermoCore.Core.Graph;

public enum PhysicalDomain
{
    MoistAir,
    DryAir,
    WaterVapor,
    LiquidWater,
    Heat,
    Electricity,
    SolarRadiation,
    MechanicalFlow,
    ControlSignal
}

public enum PortDirection
{
    Input,
    Output,
    Bidirectional
}
