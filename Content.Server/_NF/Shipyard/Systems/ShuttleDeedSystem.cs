using Content.Server._NF.Shipyard.Systems;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Examine;

namespace Content.Shared._NF.Shipyard;

public sealed partial class ShuttleDeedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShuttleDeedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ShuttleDeedComponent> ent, ref ExaminedEvent args)
    {
        var comp = ent.Comp;
        if (string.IsNullOrEmpty(comp.ShuttleName))
            return; // If shuttle name is empty, Short-Circuit.
        var fullName = ShipyardSystem.GetFullName(comp);
        args.PushMarkup(Loc.GetString("shuttle-deed-examine-text", ("shipname", fullName)));
    }
}
