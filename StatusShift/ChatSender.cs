using System;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace StatusShift;

internal static class ChatSender
{
    public static bool TrySendCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        if (!command.StartsWith('/'))
            command = "/" + command;

        try
        {
            unsafe
            {
                var ui = UIModule.Instance();
                if (ui == null)
                    return false;

                var message = Utf8String.FromString(command);
                if (message == null)
                    return false;

                ui->ProcessChatBoxEntry(message);
                message->Dtor(true);
                return true;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to send command: {Command}", command);
            return false;
        }
    }

    public static string? ToStatusCommand(OnlineStatusAction action) => action switch
    {
        OnlineStatusAction.Online => "/busy off",
        OnlineStatusAction.Roleplaying => "/roleplaying on",
        OnlineStatusAction.Busy => "/busy on",
        OnlineStatusAction.Away => "/away on",
        OnlineStatusAction.LookingForParty => "/lookingforparty on",
        _ => null,
    };
}
