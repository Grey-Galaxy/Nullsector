using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Mono.SpaceArtillery.Components;

[RegisterComponent]
public sealed partial class SpaceArtilleryComponent : Component
{

    /// <summary>
    /// Amount of power being used when operating
    /// </summary>
    [DataField("powerUsePassive"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerUsePassive = 600;

    /// <summary>
    /// Rate of charging the battery
    /// </summary>
    [DataField("powerChargeRate"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerChargeRate = 3000;

    /// <summary>
    /// Amount of power used when firing
    /// </summary>
    [DataField("powerUseActive"), ViewVariables(VVAccess.ReadWrite)]
    public int PowerUseActive = 6000;


    ///Sink Ports
    /// <summary>
    /// Signal port that makes space artillery fire.
    /// </summary>
    [DataField("spaceArtilleryFirePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string SpaceArtilleryFirePort = "SpaceArtilleryFire";


    /// <summary>
    /// The ship weapon's classification by size.
    /// </summary>
    [DataField("gunClass") /*It's a bit iffy making this required, as it'll break maps if it's not explicit in the prototype. -Z*/]
    public ShipGunClass Class = ShipGunClass.Medium;

    /// <summary>
    /// The type of damage that the ship's weapon deals.
    /// </summary>
    [DataField("gunType") /*It's a bit iffy making this required, as it'll break maps if it's not explicit in the prototype. -Z*/]
    public ShipGunType GunType = ShipGunType.Ballistic;
}

/// <summary>
/// Classes of ship guns
/// </summary>
[Serializable, NetSerializable]
public enum ShipGunClass
{
    SuperLight,
    Light,
    Medium,
    Heavy,
    SuperHeavy,
}

/// <summary>
/// Types of ship guns
/// </summary>
[Serializable, NetSerializable]
public enum ShipGunType
{
    Ballistic,
    Energy,
    Missile
}
