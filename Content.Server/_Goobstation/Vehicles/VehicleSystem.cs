using Content.Server._Mono.Radar;
using Content.Shared._Goobstation.Vehicles;
using Content.Shared._Mono.Radar;
using Content.Shared.Buckle.Components;

// Frontier: migrate under _Goobstation
// Frontier

// Frontier
    // Frontier: migrate under _Goobstation
namespace Content.Server._Goobstation.Vehicles;

public sealed class VehicleSystem : SharedVehicleSystem
{
    //// Frontier: extra logic (radar blips, faction stuff)
    [Dependency] private readonly RadarBlipSystem _radar = default!;

    /// <summary>
    /// Configures the radar blip for a vehicle entity.
    /// </summary>
    protected override void OnStrapped(Entity<VehicleComponent> ent, ref StrappedEvent args)
    {
        base.OnStrapped(ent, ref args);
        _radar.SetupVehicleRadarBlip(ent);
    }

    protected override void OnUnstrapped(Entity<VehicleComponent> ent, ref UnstrappedEvent args)
    {
        RemComp<RadarBlipComponent>(ent);
        base.OnUnstrapped(ent, ref args);
    }
    // End Frontier
}
