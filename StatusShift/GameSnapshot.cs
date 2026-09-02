using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace StatusShift;

public sealed record GameSnapshot(
    uint TerritoryId,
    string TerritoryName,
    string RegionName,
    string ZoneGroupName,
    uint JobId,
    string JobAbbr,
    uint WorldId,
    string WorldName,
    string HomeWorldName,
    HousingAddress Housing,
    DateTime Now,
    HashSet<ActivityFlag> Activities)
{
    public bool InResidence => Housing.Kind != ResidenceKind.None;

    public string Fingerprint =>
        $"{TerritoryId}|{JobId}|{WorldId}|{Housing.Summary}|{string.Join(',', Activities.Order())}";

    public static GameSnapshot Capture()
    {
        var flags = new HashSet<ActivityFlag>();
        if (Plugin.Condition[ConditionFlag.BoundByDuty])
        {
            flags.Add(ActivityFlag.InDuty);
            flags.Add(ActivityFlag.BoundByDuty);
        }
        if (Plugin.Condition[ConditionFlag.InCombat]) flags.Add(ActivityFlag.InCombat);
        if (Plugin.Condition[ConditionFlag.Crafting]) flags.Add(ActivityFlag.Crafting);
        if (Plugin.Condition[ConditionFlag.Gathering]) flags.Add(ActivityFlag.Gathering);
        if (Plugin.Condition[ConditionFlag.Mounted]) flags.Add(ActivityFlag.Mounted);
        if (Plugin.Condition[ConditionFlag.InFlight]) flags.Add(ActivityFlag.Flying);
        if (Plugin.Condition[ConditionFlag.Swimming]) flags.Add(ActivityFlag.Swimming);
        if (Plugin.Condition[ConditionFlag.Diving]) flags.Add(ActivityFlag.Diving);
        if (Plugin.Condition[ConditionFlag.WatchingCutscene] || Plugin.Condition[ConditionFlag.OccupiedInCutSceneEvent])
            flags.Add(ActivityFlag.WatchingCutscene);
        if (Plugin.Condition[ConditionFlag.Unconscious]) flags.Add(ActivityFlag.Dead);
        if (Plugin.Condition[ConditionFlag.WaitingForDutyFinder] || Plugin.Condition[ConditionFlag.UsingPartyFinder])
            flags.Add(ActivityFlag.WaitingForDutyFinder);
        if (Plugin.PartyList.Length > 0) flags.Add(ActivityFlag.InParty);
        if (Plugin.ClientState.IsPvP) flags.Add(ActivityFlag.PvP);
        if (Plugin.Condition[ConditionFlag.Casting]) flags.Add(ActivityFlag.Casting);
        if (Plugin.Condition[ConditionFlag.Jumping]) flags.Add(ActivityFlag.Jumping);
        if (Plugin.Condition[ConditionFlag.Occupied] || Plugin.Condition[ConditionFlag.OccupiedInEvent] || Plugin.Condition[ConditionFlag.OccupiedInQuestEvent])
            flags.Add(ActivityFlag.Occupied);
        if (Plugin.Condition[ConditionFlag.Occupied30] || Plugin.Condition[ConditionFlag.Occupied38] || Plugin.Condition[ConditionFlag.Occupied39])
            flags.Add(ActivityFlag.Sitting);
        if (Plugin.Condition[ConditionFlag.TradeOpen]) flags.Add(ActivityFlag.Trading);
        if (Plugin.Condition[ConditionFlag.BetweenAreas] || Plugin.Condition[ConditionFlag.BetweenAreas51])
            flags.Add(ActivityFlag.BetweenAreas);
        if (Plugin.Condition[ConditionFlag.RolePlaying]) flags.Add(ActivityFlag.Roleplaying);

        var player = Plugin.ObjectTable.LocalPlayer;
        if (player is not null && player.StatusFlags.HasFlag(StatusFlags.WeaponOut))
            flags.Add(ActivityFlag.WeaponDrawn);

        AddTargetFlags(flags, player);
        AddLookFlags(flags, player);

        if (Plugin.PartyList.Length > 0 && player is not null)
        {
            try
            {
                for (var i = 0; i < Plugin.PartyList.Length; i++)
                {
                    var member = Plugin.PartyList[i];
                    if (member is null) continue;
                    if (string.Equals(member.Name.TextValue, player.Name.TextValue, StringComparison.Ordinal))
                    {
                        if (i == 0) flags.Add(ActivityFlag.PartyLeader);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Verbose(ex, "Party leader check failed");
            }
        }

        var place = RuleEngine.ResolvePlace(Plugin.ClientState.TerritoryType);
        var housing = HousingReader.Read(place.Name);
        if (housing.Kind != ResidenceKind.None)
            flags.Add(ActivityFlag.InResidence);

        var ps = Plugin.PlayerState;
        var job = ps.IsLoaded ? ps.ClassJob : default;
        var world = ps.IsLoaded ? ps.CurrentWorld : default;
        var home = ps.IsLoaded ? ps.HomeWorld : default;

        return new GameSnapshot(
            Plugin.ClientState.TerritoryType,
            place.Name,
            place.Region,
            place.Group,
            job.RowId,
            job.IsValid ? job.Value.Abbreviation.ToString() : "",
            world.RowId,
            world.IsValid ? world.Value.Name.ToString() : "",
            home.IsValid ? home.Value.Name.ToString() : "",
            housing,
            DateTime.Now,
            flags);
    }

    private static void AddLookFlags(HashSet<ActivityFlag> flags, IGameObject? player)
    {
        try
        {
            if (player is null) return;
            unsafe
            {
                var ch = (Character*)player.Address;
                if (!ch->DrawData.IsHatHidden)
                    flags.Add(ActivityFlag.HelmShown);
                if (!ch->DrawData.IsWeaponHidden)
                    flags.Add(ActivityFlag.WeaponShown);
                var control = Control.Instance();
                if (control != null && control->IsWalking)
                    flags.Add(ActivityFlag.Walking);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "Look/walk flags failed");
        }
    }

    private static void AddTargetFlags(HashSet<ActivityFlag> flags, IGameObject? player)
    {
        try
        {
            var target = Plugin.TargetManager.Target ?? Plugin.TargetManager.SoftTarget;
            if (target is not null && player is not null && target.EntityId != player.EntityId)
            {
                if (target.ObjectKind == ObjectKind.Pc)
                    flags.Add(ActivityFlag.TargetingPlayer);
                else if (target.ObjectKind is ObjectKind.BattleNpc or ObjectKind.EventNpc)
                    flags.Add(ActivityFlag.TargetingEnemy);
            }

            if (player is null) return;
            var meEntity = player.EntityId;
            var meObject = player.GameObjectId;
            foreach (var obj in Plugin.ObjectTable)
            {
                if (obj is null || obj.EntityId == meEntity) continue;
                if (obj.ObjectKind != ObjectKind.Pc) continue;
                if (obj.TargetObjectId == meObject || obj.TargetObject?.EntityId == meEntity)
                {
                    flags.Add(ActivityFlag.TargetedByPlayer);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "Target scan failed");
        }
    }
}
