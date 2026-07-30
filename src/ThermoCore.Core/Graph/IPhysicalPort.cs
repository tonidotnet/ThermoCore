namespace ThermoCore.Core.Graph;

public interface IPhysicalPort
{
    string Id { get; }

    string ComponentId { get; }

    PortDirection Direction { get; }

    PhysicalDomain Domain { get; }

    bool IsRequired { get; }
}
