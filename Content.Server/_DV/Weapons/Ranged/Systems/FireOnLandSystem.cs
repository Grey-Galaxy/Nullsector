using System.Numerics;
using Content.Server._DV.Weapons.Ranged.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server._DV.Weapons.Ranged.Systems;

public sealed class FireOnLandSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FireOnLandComponent, LandEvent>(FireOnLand);
    }

/*
 NOTE: If you are crashing when throwing a Bolted Gun, it is not because of this implementation, but
 an Enumeration query in the Throwing System. It should -NOT- cause any issues in practice on a server.
 -Z
 */
    /// <summary>
    /// Causes a gun entity with the FireOnLandComponent to fire when the LandEvent event is triggered.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void FireOnLand(Entity<FireOnLandComponent> entity, ref LandEvent args)
    {
        if (!TryComp(entity, out GunComponent? gunComponent))
            return;

        if (!_random.Prob(entity.Comp.Probability))
            return;

        var dir = gunComponent.DefaultDirection;
        dir = new Vector2(-dir.Y, dir.X); // 90 degrees counter-clockwise, guns shoot down by default
        var targetCoordinates = new EntityCoordinates(entity, dir);
        _gunSystem.AttemptShoot(entity, entity, gunComponent, targetCoordinates);
    }
}
