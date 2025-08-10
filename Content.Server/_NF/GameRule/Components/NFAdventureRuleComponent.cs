namespace Content.Server._NF.GameRule.Components;

[RegisterComponent, Access(typeof(NFAdventureRuleSystem))]
public sealed partial class NFAdventureRuleComponent : Component
{
    public List<EntityUid> NFPlayerMinds = [];
    public List<EntityUid> CargoDepots = [];
    public List<EntityUid> MarketStations = [];
    public List<EntityUid> RequiredPois = [];
    public List<EntityUid> OptionalPois = [];
    public List<EntityUid> UniquePois = [];
}
