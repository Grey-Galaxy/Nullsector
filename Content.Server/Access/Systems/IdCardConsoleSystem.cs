// Frontier
// Frontier

using System.Linq;
using Content.Server._NF.Shipyard.Systems;
using Content.Server.Popups;
using Content.Server.StationRecords.Systems;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using static Content.Shared.Access.Components.IdCardConsoleComponent;
using static Content.Shared._NF.Shipyard.Components.ShuttleDeedComponent; // Frontier

namespace Content.Server.Access.Systems;

[UsedImplicitly]
public sealed class IdCardConsoleSystem : SharedIdCardConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly AccessSystem _access = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ShipyardSystem _shipyard = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!; // Null
    [Dependency] private readonly ISerializationManager _serializationManager = default!; // Null
    [Dependency] private readonly ShipAutoDeleteSystem _shipAutoDelete = default!; // Null from Mono

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardConsoleComponent, SharedIdCardSystem.WriteToTargetIdMessage>(OnWriteToTargetIdMessage);
        SubscribeLocalEvent<IdCardConsoleComponent, SharedIdCardSystem.WriteToShuttleDeedMessage>(OnWriteToShuttleDeedMessage);
        SubscribeLocalEvent<IdCardConsoleComponent, SharedIdCardSystem.TransferDeedMessage>(OnTransferDeed);

        // one day, maybe bound user interfaces can be shared too.
        SubscribeLocalEvent<IdCardConsoleComponent, ComponentStartup>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<IdCardConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
    }

    private void OnWriteToTargetIdMessage(EntityUid uid,
        IdCardConsoleComponent component,
        SharedIdCardSystem.WriteToTargetIdMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        TryWriteToTargetId(uid, args.FullName, args.JobTitle, args.AccessList, args.JobPrototype, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void OnWriteToShuttleDeedMessage(EntityUid uid,
        IdCardConsoleComponent component,
        SharedIdCardSystem.WriteToShuttleDeedMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        TryWriteToShuttleDeed(uid, args.ShuttleName, args.ShuttleSuffix, player, component);

        UpdateUserInterface(uid, component, args);
    }

    private void UpdateUserInterface(EntityUid uid, IdCardConsoleComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;

        var privilegedIdName = string.Empty;
        List<ProtoId<AccessLevelPrototype>>? possibleAccess = null;
        if (component.PrivilegedIdSlot.Item is { Valid: true } item)
        {
            privilegedIdName = EntityManager.GetComponent<MetaDataComponent>(item).EntityName;
            possibleAccess = _accessReader.FindAccessTags(item).ToList();
        }

        IdCardConsoleBoundUserInterfaceState newState;
        // this could be prettier
        if (component.TargetIdSlot.Item is not { Valid: true } targetId)
        {
            newState = new IdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                false,
                null,
                null,
                false,
                null,
                null,
                possibleAccess,
                string.Empty,
                privilegedIdName,
                string.Empty);
        }
        else
        {
            var targetIdComponent = EntityManager.GetComponent<IdCardComponent>(targetId);
            var targetAccessComponent = EntityManager.GetComponent<AccessComponent>(targetId);

            var jobProto = new ProtoId<JobPrototype>(string.Empty); // Frontier: AccessLevelPrototype<JobPrototype
            if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
                && keyStorage.Key is { } key
                && _record.TryGetRecord<GeneralStationRecord>(key, out var record))
            {
                jobProto = record.JobPrototype;
            }

            string?[]? shuttleNameParts = null;
            var hasShuttle = false;
            if (EntityManager.TryGetComponent<ShuttleDeedComponent>(targetId, out var comp))
            {
                shuttleNameParts = [comp.ShuttleName, comp.ShuttleNameSuffix];
                hasShuttle = true;
            }

            newState = new IdCardConsoleBoundUserInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                PrivilegedIdIsAuthorized(uid, component),
                true,
                targetIdComponent.FullName,
                targetIdComponent.LocalizedJobTitle,
                hasShuttle, // Frontier
                shuttleNameParts, // Frontier
                targetAccessComponent.Tags.ToList(),
                possibleAccess,
                jobProto,
                privilegedIdName,
                Name(targetId));
        }

        _userInterface.SetUiState(uid, IdCardConsoleUiKey.Key, newState);
    }

    /// <summary>
    /// Called whenever an access button is pressed, adding or removing that access from the target ID card.
    /// Writes data passed from the UI into the ID stored in <see cref="IdCardConsoleComponent.TargetIdSlot"/>, if present.
    /// </summary>
    private void TryWriteToTargetId(EntityUid uid,
        string newFullName,
        string newJobTitle,
        List<ProtoId<AccessLevelPrototype>> newAccessList,
        ProtoId<JobPrototype> newJobProto, // Frontier: AccessLevelPrototype<JobPrototype
        EntityUid player,
        IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, component))
            return;

        _idCard.TryChangeFullName(targetId, newFullName, player: player);
        _idCard.TryChangeJobTitle(targetId, newJobTitle, player: player);

        if (_prototype.TryIndex(newJobProto, out var job)
            && _prototype.TryIndex(job.Icon, out var jobIcon))
        {
            _idCard.TryChangeJobIcon(targetId, jobIcon, player: player);
            _idCard.TryChangeJobDepartment(targetId, job);
        }

        UpdateStationRecord(uid, targetId, newFullName, newJobTitle, job);

        if (!newAccessList.TrueForAll(x => component.AccessLevels.Contains(x)))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to write unknown access tag.");
            return;
        }

        var oldTags = _access.TryGetTags(targetId) ?? new List<ProtoId<AccessLevelPrototype>>();
        oldTags = oldTags.ToList();

        var privilegedId = component.PrivilegedIdSlot.Item;

        if (oldTags.SequenceEqual(newAccessList))
            return;

        // I hate that C# doesn't have an option for this and don't desire to write this out the hard way.
        // var difference = newAccessList.Difference(oldTags);
        var difference = newAccessList.Union(oldTags).Except(newAccessList.Intersect(oldTags)).ToHashSet();
        // NULL SAFETY: PrivilegedIdIsAuthorized checked this earlier.
        var privilegedPerms = _accessReader.FindAccessTags(privilegedId!.Value).ToHashSet();
        if (!difference.IsSubsetOf(privilegedPerms))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to modify permissions they could not give/take!");
            return;
        }

        var addedTags = newAccessList.Except(oldTags).Select(tag => "+" + tag).ToList();
        var removedTags = oldTags.Except(newAccessList).Select(tag => "-" + tag).ToList();
        _access.TrySetTags(targetId, newAccessList);

        /*TODO: ECS SharedIdCardConsoleComponent and then log on card ejection, together with the save.
        This current implementation is pretty shit as it logs 27 entries (27 lines) if someone decides to give themselves AA*/
        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(player):player} has modified {ToPrettyString(targetId):entity} with the following accesses: [{string.Join(", ", addedTags.Union(removedTags))}] [{string.Join(", ", newAccessList)}]");
    }

    /// <summary>
    /// Called whenever an attempt to change the shuttle deed of the target id is made.
    /// Writes data passed from the ui to the shuttle deed and the grid of shuttle.
    /// </summary>
    private void TryWriteToShuttleDeed(EntityUid uid,
        string newShuttleName,
        string newShuttleSuffix,
        EntityUid player,
        IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, component))
            return;

        if (!EntityManager.TryGetComponent<ShuttleDeedComponent>(targetId, out var shuttleDeed))
            return;
        else
        {
            if (Deleted(shuttleDeed!.ShuttleUid))
            {
                RemComp<ShuttleDeedComponent>(targetId);
                return;
            }
        }

        // Ensure the name is valid and follows the convention
        var name = newShuttleName.Trim();
        // The suffix is ignored as per request
        // var suffix = newShuttleSuffix;
        var suffix = shuttleDeed.ShuttleNameSuffix;

        if (name.Length > MaxNameLength)
            name = name[..MaxNameLength];
        // if (suffix.Length > MaxSuffixLength)
        //     suffix = suffix[..MaxSuffixLength];

        _shipyard.TryRenameShuttle(targetId, shuttleDeed, name, suffix);

        _adminLogger.Add(LogType.Action,
            LogImpact.Medium,
            $"{ToPrettyString(player):player} has changed the shuttle name of {ToPrettyString(shuttleDeed.ShuttleUid):entity} to {ShipyardSystem.GetFullName(shuttleDeed)}");
    }

    /// <summary>
    /// Returns true if there is an ID in <see cref="IdCardConsoleComponent.PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReaderComponent"/>.
    /// </summary>
    /// <remarks>
    /// Other code relies on the fact this returns false if privileged ID is null. Don't break that invariant.
    /// </remarks>
    private bool PrivilegedIdIsAuthorized(EntityUid uid, IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        if (!TryComp<AccessReaderComponent>(uid, out var reader))
            return true;

        var privilegedId = component.PrivilegedIdSlot.Item;
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, reader);
    }

    private void UpdateStationRecord(EntityUid uid,
        EntityUid targetId,
        string newFullName,
        ProtoId<AccessLevelPrototype> newJobTitle,
        JobPrototype? newJobProto)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.Name = newFullName;
        record.JobTitle = newJobTitle;

        if (newJobProto != null)
        {
            record.JobPrototype = newJobProto.ID;
            record.JobIcon = newJobProto.Icon;
        }

        _record.Synchronize(key);
    }

    /*
     * As of 07/22/2025 code below this point is by the Null Sector. This can change later, I suppose.
     * Frankly,  I don't care to torment myself to accredit all of it.
     * It's my work. I did it, and it (hopefully) accomplishes what I want it to. You want to use it? Go nuts.
     * The license for the repository means you can't make it private, and that's good 'nuff. If you find a flaw,
     * please for the love of god, tell me. I am more than happy to contribute what I know, and my thoughts on
     * parts of the code.
     */

    /// <summary>
    /// If the component is present, the privileged and target slots hold valid id's, the user is authorized,
    /// the user is indeed a player, the privileged ID has a valid deed, and the target does not have a deed
    /// though if it did and the ship was deleted that's okay, then a deed is transferred from the privileged ID
    /// to the target.
    /// </summary>
    private void OnTransferDeed(EntityUid uid,
        IdCardConsoleComponent? idCardConsoleComponent,
        SharedIdCardSystem.TransferDeedMessage arguments)
    {
        // If the component isn't present, short-circuit.
        if (!Resolve(uid, ref idCardConsoleComponent))
            return;

        // If the item in either Privileged or Target slots are not Valid, or isn't authorized, short-circuit.
        if (
            idCardConsoleComponent.PrivilegedIdSlot.Item is not { Valid: true } privilagedId
            || idCardConsoleComponent.TargetIdSlot.Item is not { Valid: true } targetId
            || !PrivilegedIdIsAuthorized(uid, idCardConsoleComponent))
            return;

        // If the actor working with the console isn't a player, short-circuit.
        if (arguments.Actor is not { Valid: true } player)
            return;

        // If the privilegedId does not have a deed, short-circuit.
        if (!EntityManager.TryGetComponent<ShuttleDeedComponent>(privilagedId, out var privilagedDeed))
        {
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to give a deed without having one!");
            _popupSystem.PopupEntity(Loc.GetString("transfer-deed-no-deed"), uid, arguments.Actor);
            return;
        }
        // If the privilaged ID's deed's shuttle is actually nonexistent, then remove the deed from the id.
        // It's gone, man!
        var privilagedDeedsShuttle = privilagedDeed.ShuttleUid!.Value;
        if (Deleted(privilagedDeedsShuttle))
        {
            RemComp<ShuttleDeedComponent>(privilagedId);
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to assign a dead deed to an ID!");
            _popupSystem.PopupEntity(Loc.GetString("transfer-deed-original-shuttle-gone"), uid, arguments.Actor);
            return;
        }

        // If the target ID ALREADY HAS A DEED, short-circuit.
        if (EntityManager.TryGetComponent<ShuttleDeedComponent>(targetId, out var targetDeed))
        {
            // If the target ID has a dead, but it's dead, then remove the deed from the id. This is done beforehand.
            // It's gone, man!
            if (Deleted(targetDeed!.ShuttleUid))
            {
                RemComp<ShuttleDeedComponent>(targetId);
                goto DEAD_ID_BYPASS;
            }
            _sawmill.Warning($"User {ToPrettyString(uid)} tried to assign a deed to an ID that already had one!");
            _popupSystem.PopupEntity(Loc.GetString("transfer-deed-target-has-deed"), uid, arguments.Actor);
            return;
        }
        // A brutally simple GOTO to ship all the heartache if the target already had a deed, but it got deleted.
        DEAD_ID_BYPASS:
        TransferDeedToTarget(privilagedDeed, privilagedId, targetId, arguments.Actor);
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(player):player} has transferred deed of {ToPrettyString(privilagedDeed.ShuttleUid):entity}.");
        UpdateUserInterface(uid, idCardConsoleComponent, arguments);
    }

    /// <summary>
    /// Copy deed properties from source to target ID card.
    /// </summary>
    private void TransferDeedToTarget(ShuttleDeedComponent sourceDeed, EntityUid sourceId, EntityUid targetId, EntityUid user)
    {
        // Remove any existing from the target ID first, just in case.
        RemComp<ShuttleDeedComponent>(targetId);

        if (sourceDeed.ShuttleUid == null)
            return; // If the shuttle Uid is null, something is horribly wrong.

        // Remove Auto-Delete Component from the source deed's shuttle. We'll add it back, later.
        RemComp<ShipAutoDeleteComponent>(sourceDeed.ShuttleUid.Value);

        // Create a deep copy of the source deed component using serialization and set deed holder ID card.
        var copiedDeed = _serializationManager.CreateCopy(sourceDeed, notNullableOverride: true);

        // It will always be an ID. Just set the deed holder to the new ID, and the Shuttle Owner.
        var idCardComponent = Comp<IdCardComponent>(targetId);
        copiedDeed.DeedHolderCard = targetId;
        copiedDeed.ShuttleOwner = idCardComponent.FullName;

        // Now adjust the deed values on the ship itself.
        if (TryComp<ShuttleDeedComponent>(copiedDeed.ShuttleUid!.Value, out var shuttleDeed))
        {
            shuttleDeed.ShuttleOwner = idCardComponent.FullName;
            shuttleDeed.DeedHolderCard = targetId;
        }

        // NULL SECTOR : Removed ShipAutoDelete, for now.
        /*// Add auto-delete component to shuttle for if it ever went derelict.
        // Auto-cleanup should handle preventing deleting people.
        if (TryComp<ActorComponent>(copiedDeed.DeedHolderCard, out var actorComp))
        {
            _shipAutoDelete.RegisterAutoDelete(copiedDeed.ShuttleUid!.Value, actorComp.PlayerSession);
        }*/


        // Add the copied component to the target entity
        EntityManager.AddComponent(targetId, copiedDeed, overwrite: true);

        // Remove the deed from the original ID.
        RemComp<ShuttleDeedComponent>(sourceId);

        _popupSystem.PopupEntity(
            Loc.GetString("transfer-deed-success",
                ("ship", copiedDeed.ShuttleName ?? "Unknown"),
                ("holder", copiedDeed.ShuttleOwner ?? "Unknown")
            ),
            targetId,
            user);
    }

}
