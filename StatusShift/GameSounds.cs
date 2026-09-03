using System;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace StatusShift;

internal static class GameSounds
{
    public static void Play(int id)
    {
        try
        {
            var n = (uint)Math.Clamp(id, 1, 16);
            unsafe { UIGlobals.PlayChatSoundEffect(n); }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "Ping failed");
        }
    }

    public static void ConfirmPing() => Play(1);
}
