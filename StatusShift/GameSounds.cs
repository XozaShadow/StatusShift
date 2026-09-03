using System;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace StatusShift;

internal static class GameSounds
{
    public static void ConfirmPing()
    {
        try
        {
            unsafe { UIGlobals.PlayChatSoundEffect(1); }
        }
        catch (Exception ex)
        {
            Plugin.Log.Verbose(ex, "Ping failed");
        }
    }
}
