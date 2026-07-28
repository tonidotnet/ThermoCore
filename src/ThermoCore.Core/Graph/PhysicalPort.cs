namespace ThermoCore.Core.Graph;

public interface IPhysicalPort
{
    string Id { get; }

    string ComponentId { get; }

    PortDirection Direction { get; }

    PhysicalDomain Domain { get; }

    bool IsRequired { get; }
}

public sealed class PhysicalPort : IPhysicalPort
{
    public PhysicalPort(
        string id,
        string componentId,
        PortDirection direction,
        PhysicalDomain domain,
        bool isRequired = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(componentId);

        Id = id;
        ComponentId = componentId;
        Direction = direction;
        Domain = domain;
        IsRequired = isRequired;
    }

    public string Id { get; }

    public string ComponentId { get; }

    public PortDirection Direction { get; }

    public PhysicalDomain Domain { get; }

    public bool IsRequired { get; }
}
