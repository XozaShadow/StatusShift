using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;

namespace StatusShift;

internal sealed class LiveLook
{
    public List<string> NearbyPlayers { get; } = [];
    public string MountName { get; init; } = string.Empty;
    public string EmoteName { get; init; } = string.Empty;
    public string AccessoryName { get; init; } = string.Empty;
    public string TargeterName { get; init; } = string.Empty;
    public bool Mounted { get; init; }

    public static LiveLook Capture(float range)
    {
        var look = new LiveLook();
        var player = Plugin.ObjectTable.LocalPlayer;
        if (player is null) return look;

        try
        {
            foreach (var obj in Plugin.ObjectTable)
            {
                if (obj is null || obj.ObjectKind != ObjectKind.Pc) continue;
                if (obj.EntityId == player.EntityId) continue;
                var dist = Vector3.Distance(player.Position, obj.Position);
                if (dist > range) continue;
                var name = obj.Name.TextValue;
                if (string.IsNullOrWhiteSpace(name)) continue;
                var world = string.Empty;
                if (obj is IPlayerCharacter pc)
                {
                    try
                    {
                        var w = pc.HomeWorld;
                        if (w.IsValid) world = w.Value.Name.ToString();
                    }
                    catch { /* ignore */ }
                }
                look.NearbyPlayers.Add(string.IsNullOrEmpty(world) ? name : $"{name}@{world}");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "Nearby scan failed");
        }

        var mount = string.Empty;
        var emote = string.Empty;
        var targeter = string.Empty;
        var mounted = Plugin.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted];
        try
        {
            unsafe
            {
                var ch = (Character*)player.Address;
                var mountId = ch->Mount.MountId;
                if (mountId != 0)
                {
                    mounted = true;
                    var sheet = Plugin.DataManager.GetExcelSheet<Mount>();
                    var row = sheet?.GetRowOrDefault(mountId);
                    if (row is not null)
                        mount = row.Value.Singular.ToString();
                    if (string.IsNullOrWhiteSpace(mount))
                        mount = mountId.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "Mount read failed");
        }

        try
        {
            var meEntity = player.EntityId;
            var meObject = player.GameObjectId;
            foreach (var obj in Plugin.ObjectTable)
            {
                if (obj is null || obj.EntityId == meEntity) continue;
                if (obj.ObjectKind != ObjectKind.Pc) continue;
                if (obj.TargetObjectId == meObject || obj.TargetObject?.EntityId == meEntity)
                {
                    targeter = obj.Name.TextValue;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "Targeter scan failed");
        }

        return new LiveLook
        {
            MountName = mount,
            EmoteName = emote,
            TargeterName = targeter,
            Mounted = mounted,
            Nearby = look.NearbyPlayers,
        };
    }

    public List<string>? Nearby { init { if (value is not null) NearbyPlayers.AddRange(value); } }
}
