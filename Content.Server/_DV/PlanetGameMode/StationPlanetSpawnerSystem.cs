using Content.Server._DeltaV.Planet;
using Content.Server._DV.PlanetGameMode;
using Content.Shared.Coordinates;
using Robust.Shared.Map.Components;

namespace Content.Server._DeltaV.Station.Systems;

public sealed class StationPlanetSpawnerSystem : EntitySystem
{
    [Dependency] private readonly PlanetSystem _planet = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationPlanetSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StationPlanetSpawnerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<StationPlanetSpawnerComponent> ent, ref MapInitEvent args)
    {
        // if (ent.Comp.GridPath is not {} path)
        //     return;

        if (ent.Comp.IsTerrestrialStation)
        {
            if (!HasComp<MapGridComponent>(ent.Owner))
            {
                Log.Error("Attempting to load a terrestrial station without a grid.");
                return;
            }

            var mapGridComponent = Comp<MapGridComponent>(ent.Owner);
            var grid = new Entity<MapGridComponent>(ent.Owner, mapGridComponent);

            var mapId = _transform.GetMapId(ent.Owner.ToCoordinates());
            var map = _map.GetMap(mapId);
            _planet.MakePlanet(grid, ent.Comp.Planet, map);
        }
        else
        {
            if (ent.Comp.GridPath is not { } path)
            {
                Log.Warning("Attempting to spawn planet map without a specified grid path.");
                return;
            }
            ent.Comp.Map = _planet.LoadPlanet(ent.Comp.Planet, path);
        }
    }

    private void OnShutdown(Entity<StationPlanetSpawnerComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.Map);
    }
}
