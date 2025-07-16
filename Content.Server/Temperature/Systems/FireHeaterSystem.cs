using Content.Server.Temperature.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Placeable;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles <see cref="FireHeaterComponent"/> updating and events. Free code made by Lukezurg22 for Null Sector.
/// </summary>
public sealed class FireHeaterSystem : EntitySystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<FireHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out _, out var heater, out var placer))
        {
            var heatChange = heater.HeatPerSecond * frameTime + 0.1f;
            foreach (var entity in placer.PlacedEntities)
            {
                if (TryComp<TemperatureComponent>(entity, out var temperatureComponent))
                {
                    _temperature.ChangeHeat(entity, heatChange, true, temperatureComponent);
                }

                // If the placed item is not a container, add some heat to that sucker.
                if (!TryComp<SolutionContainerManagerComponent>(entity, out var container))
                    continue; // Short-circuit; there is no SolutionContainerManagerComponent.

                // Since the placed entity is indeed a container, heat that bad boy up.
                foreach (var (_, solution) in _solutionContainer.EnumerateSolutions((entity, container)))
                {
                    _solutionContainer.AddThermalEnergy(solution, heatChange);
                }
            }
        }
    }
}
