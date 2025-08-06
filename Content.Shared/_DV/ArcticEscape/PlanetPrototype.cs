using Content.Shared.Atmos;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Prototypes;

namespace Content.Shared._DeltaV.Planet;

[Prototype]
public sealed partial class PlanetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// The biome to create the planet with.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome;

    /// <summary>
    /// Name to give to the map.
    /// </summary>
    [DataField(required: true)]
    public LocId MapName;

    /// <summary>
    /// Ambient lighting for the map.
    /// </summary>
    [DataField]
    public Color MapLight = Color.FromHex("#D8B059");

    /// <summary>
    /// Daylight color for lighting for the map.
    /// </summary>
    [DataField]
    public Color Sunlight = Color.FromHex("#F9F1D8");

    /// <summary>
    /// Moonlight color for lighting for the map.
    /// </summary>
    [DataField]
    public Color Moonlight = Color.FromHex("#59617D");

    /// <summary>
    ///  The highest lighting level to use when lighting the planet during the day.
    /// </summary>
    [DataField]
    public float ClipLight = 1.25f;

    /// <summary>
    ///  The lowest lighting level to use when lighting the planet at night.
    /// </summary>
    [DataField]
    public float MinimumLightLevel = 0f;

    /// <summary>
    ///  The highest lighting level to use when lighting the planet during the day.
    /// </summary>
    [DataField]
    public float MaximumLightLevel = 3f;

    /// <summary>
    /// Components to add to the map.
    /// </summary>
    [DataField]
    public ComponentRegistry? AddedComponents;

    /// <summary>
    /// The gas mixture to use for the atmosphere.
    /// </summary>
    [DataField(required: true)]
    public GasMixture Atmosphere = new();

    /// <summary>
    /// Biome layers to add to the map, i.e. ores.
    /// </summary>
    [DataField]
    public List<ProtoId<BiomeMarkerLayerPrototype>> BiomeMarkerLayers = new();
}
