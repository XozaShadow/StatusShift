using System;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace StatusShift;

internal static class ChatSender
{
    public static readonly string[] StatusLabels =
    [
        "Leave alone",
        "Online",
        "Away from Keyboard",
        "Busy",
        "Role-playing",
        "Looking to Meld Materia",
        "Looking for Party",
        "Mentor",
        "PvE Mentor",
        "PvP Mentor",
        "Trade Mentor",
        "Returner",
        "New Adventurer",
    ];

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
        OnlineStatusAction.Away => "/away on",
        OnlineStatusAction.Busy => "/busy on",
        OnlineStatusAction.Roleplaying => "/roleplaying on",
        OnlineStatusAction.LookingToMeld => "/lookingtomeld on",
        OnlineStatusAction.LookingForParty => "/lookingforparty on",
        OnlineStatusAction.Mentor => "/mentor on",
        OnlineStatusAction.PveMentor => "/pvementor on",
        OnlineStatusAction.PvpMentor => "/pvpmentor on",
        OnlineStatusAction.TradeMentor => "/tradementor on",
        OnlineStatusAction.Returner => "/returner on",
        OnlineStatusAction.NewAdventurer => "/beginner on",
        _ => null,
    };
}
