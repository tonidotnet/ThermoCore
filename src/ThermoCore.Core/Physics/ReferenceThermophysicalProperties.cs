namespace ThermoCore.Core.Physics;

/// <summary>
/// Temperature-independent engineering approximations for initial ThermoCore models.
/// See docs/02_Mathematics/26_Constants.md.
/// </summary>
public static class ReferenceThermophysicalProperties
{
    /// <summary>Dry-air specific heat. TemperatureDependentApproximation.</summary>
    public const double DryAirSpecificHeatJPerKgK = 1006.0;

    /// <summary>Water-vapor specific heat. TemperatureDependentApproximation.</summary>
    public const double WaterVaporSpecificHeatJPerKgK = 1860.0;

    /// <summary>Liquid-water specific heat. TemperatureDependentApproximation.</summary>
    public const double LiquidWaterSpecificHeatJPerKgK = 4180.0;

    /// <summary>Reference vaporization enthalpy at the moist-air enthalpy reference state.</summary>
    public const double ReferenceVaporizationEnthalpyJPerKg = 2_501_000.0;
    /// <summary>Dry-air dynamic viscosity near room temperature. EngineeringReference.</summary>
    public const double DryAirDynamicViscosityPaS = 1.81e-5;
}
