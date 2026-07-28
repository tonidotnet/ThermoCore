namespace ThermoCore.Core.Physics;

/// <summary>
/// Shared physical constants used across ThermoCore. Values and units follow docs/02_Mathematics/26_Constants.md.
/// </summary>
public static class PhysicalConstants
{
    /// <summary>Offset from Celsius to kelvin. ExactDefinition.</summary>
    public const double CelsiusOffsetK = 273.15;

    /// <summary>Standard atmospheric pressure. InternationalRecommended.</summary>
    public const double StandardAtmosphericPressurePa = 101_325.0;

    /// <summary>Universal gas constant. InternationalRecommended.</summary>
    public const double UniversalGasConstantJPerMolK = 8.31446261815324;

    /// <summary>Specific gas constant for dry air. EngineeringReference.</summary>
    public const double DryAirGasConstantJPerKgK = 287.055;

    /// <summary>Specific gas constant for water vapor. EngineeringReference.</summary>
    public const double WaterVaporGasConstantJPerKgK = 461.52;

    /// <summary>Molecular mass ratio ε = M_v / M_da. EngineeringReference.</summary>
    public const double MolecularMassRatio = 0.621945;

    /// <summary>Stefan–Boltzmann constant. InternationalRecommended.</summary>
    public const double StefanBoltzmannConstantWPerM2K4 = 5.670374419e-8;

    /// <summary>Standard gravity. ExactDefinition / InternationalRecommended.</summary>
    public const double StandardGravityMPerS2 = 9.80665;

    /// <summary>Reference liquid-water density near room temperature. EngineeringReference.</summary>
    public const double WaterDensityReferenceKgPerM3 = 997.0;
}
