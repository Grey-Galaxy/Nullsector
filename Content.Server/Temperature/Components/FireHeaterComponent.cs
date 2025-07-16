using Content.Server.Temperature.Systems;

namespace Content.Server.Temperature.Components;

/// <summary>
/// Adds thermal energy to entities with <see cref="TemperatureComponent"/> placed on it.
/// </summary>
[RegisterComponent, Access(typeof(FireHeaterSystem))]
public sealed partial class FireHeaterComponent : Component
{
    /// <summary>
    /// How much heat is added per second to the solution, taking upgrades into account.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatPerSecond = 240;
}
