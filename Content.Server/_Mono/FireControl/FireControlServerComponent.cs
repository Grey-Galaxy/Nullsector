// SPDX-FileCopyrightText: 2025 Ark
// SPDX-FileCopyrightText: 2025 RikuTheKiller
// SPDX-FileCopyrightText: 2025 ark1368
// SPDX-FileCopyrightText: 2025 sleepyyapril
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Copyright Rane (elijahrane@gmail.com) 2025
// All rights reserved. Relicensed under AGPL with permission

namespace Content.Server._Mono.FireControl;

[RegisterComponent]
public sealed partial class FireControlServerComponent : Component
{
    [ViewVariables]
    public EntityUid? ConnectedGrid = null;

    [ViewVariables]
    public HashSet<EntityUid> Controlled = [];

    [ViewVariables]
    public HashSet<EntityUid> Consoles = [];

    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> Leases;

    /*
    [ViewVariables, DataField]
    public int ProcessingPower;

    [ViewVariables]
    public int UsedProcessingPower;

    //LOCALIZATION:
    //gunnery-server-examine-detail = The server is using [color={$valueColor}]{$usedProcessingPower}/{$processingPower}[/color] of its processing power.
    // Though this is a really cool system and all, it DOESN'T WORK very well. It is more of a nuisance than a benefit.
    // The calculations for processing power aren't finished either. Though it would be easy to finish them, we
    // here in the Null Sector aren't big fans.
    */

    [ViewVariables, DataField]
    public int MaxConsoles = 1;

    [ViewVariables, DataField]
    public bool EnforceMaxConsoles;
}
