using Content.Server.Cargo.Systems;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Server.Xenoarchaeology.Artifact;

/// <inheritdoc cref="SharedXenoArtifactSystem"/>
public sealed partial class XenoArtifactSystem : SharedXenoArtifactSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoArtifactComponent, MapInitEvent>(OnArtifactMapInit);
        SubscribeLocalEvent<XenoArtifactComponent, PriceCalculationEvent>(OnCalculatePrice);
    }

    private void OnArtifactMapInit(Entity<XenoArtifactComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.IsGenerationRequired)
            GenerateArtifactStructure(ent);
    }

    /// <summary>
    /// Calculates the price of an artifact based on how many nodes have been unlocked and triggered.
    /// </summary>
    /// <remarks>
    /// General balancing (for fully unlocked artifacts):<br/>
    /// Simple (1-2 Nodes): 1-2K<br/>
    /// Medium (5-8 Nodes): 6-7K<br/>
    /// Complex (7-12 Nodes): 10-11K<br/>
    /// </remarks>
    private void OnCalculatePrice(Entity<XenoArtifactComponent> ent, ref PriceCalculationEvent args)
    {
        foreach (var node in GetAllNodes(ent))
        {
            if (node.Comp.Locked)
                continue;

            args.Price += node.Comp.ResearchValue * ent.Comp.PriceMultiplier;
        }
    }
}
