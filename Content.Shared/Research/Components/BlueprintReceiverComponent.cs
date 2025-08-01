using Content.Shared.Research.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Research.Components;

/// <summary>
/// This is used for a lathe that can utilize <see cref="BlueprintComponent"/>s to gain more recipes.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(BlueprintSystem))]
public sealed partial class BlueprintReceiverComponent : Component
{
    [DataField]
    public string ContainerId = "blueprint";

    // Whitelisted blueprints are, limiting certain blueprints to certain lathes, but all blueprints are given a basic tag to accomodate.
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    // Blacklisted blueprints are not required, but could prevent specific blueprints from entering lathes.
    [DataField(required: false)]
    public EntityWhitelist Blacklist = new();
    /*
     *  ===HOW TO USE===
        - type: BlueprintReceiver
        whitelist:
          tags:
          - BlueprintAutolathe
        whitelist:
          tags:
          - <tag>
     */
}
