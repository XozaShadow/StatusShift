using System;
using Dalamud.Game.Text;

namespace StatusShift;

internal static class ChatWatch
{
    public static string LastTellFrom { get; private set; } = string.Empty;
    public static string LastChatSender { get; private set; } = string.Empty;
    public static string LastChatChannel { get; private set; } = string.Empty;
    public static string LastChatText { get; private set; } = string.Empty;
    public static DateTime LastTellAt { get; private set; }
    public static DateTime LastChatAt { get; private set; }

    public static bool TellFresh => LastTellFrom.Length > 0 && (DateTime.Now - LastTellAt).TotalSeconds < 30;
    public static bool ChatFresh => LastChatChannel.Length > 0 && (DateTime.Now - LastChatAt).TotalSeconds < 20;

    public static void Attach() => Plugin.Chat.ChatMessage += OnChat;
    public static void Detach() => Plugin.Chat.ChatMessage -= OnChat;

    private static void OnChat(IHandleableChatMessage message)
    {
        var type = message.LogKind;
        var channel = ChannelName(type);
        if (channel.Length == 0) return;
        var who = message.Sender.TextValue.Trim();
        var text = message.Message.TextValue.Trim();
        LastChatChannel = channel;
        LastChatSender = who;
        LastChatText = text;
        LastChatAt = DateTime.Now;
        if (type is XivChatType.TellIncoming)
        {
            LastTellFrom = who;
            LastTellAt = DateTime.Now;
        }
    }

    public static bool TellFrom(string value)
    {
        if (!TellFresh) return false;
        value = (value ?? string.Empty).Trim();
        if (value.Length == 0) return true;
        return LastTellFrom.Equals(value, StringComparison.OrdinalIgnoreCase)
               || LastTellFrom.StartsWith(value + "@", StringComparison.OrdinalIgnoreCase)
               || LastTellFrom.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ChatMatches(string value)
    {
        if (!ChatFresh) return false;
        value = (value ?? string.Empty).Trim();
        if (value.Length == 0) return true;
        var bar = value.IndexOf('|');
        var channel = bar < 0 ? value : value[..bar].Trim();
        var text = bar < 0 ? string.Empty : value[(bar + 1)..].Trim();
        if (channel.Length > 0 && !LastChatChannel.Equals(channel, StringComparison.OrdinalIgnoreCase))
            return false;
        if (text.Length > 0 && !LastChatText.Contains(text, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    public static string ChannelName(XivChatType type) => type switch
    {
        XivChatType.Say => "Say",
        XivChatType.Shout => "Shout",
        XivChatType.Yell => "Yell",
        XivChatType.TellIncoming => "Tell",
        XivChatType.TellOutgoing => "Tell Out",
        XivChatType.Party => "Party",
        XivChatType.Alliance => "Alliance",
        XivChatType.FreeCompany => "Free Company",
        XivChatType.NoviceNetwork => "Novice Network",
        XivChatType.CustomEmote => "Custom Emote",
        XivChatType.StandardEmote => "Emote",
        XivChatType.Echo => "Echo",
        XivChatType.PvPTeam => "PvP Team",
        XivChatType.Ls1 => "Linkshell 1",
        XivChatType.Ls2 => "Linkshell 2",
        XivChatType.Ls3 => "Linkshell 3",
        XivChatType.Ls4 => "Linkshell 4",
        XivChatType.Ls5 => "Linkshell 5",
        XivChatType.Ls6 => "Linkshell 6",
        XivChatType.Ls7 => "Linkshell 7",
        XivChatType.Ls8 => "Linkshell 8",
        XivChatType.CrossLinkShell1 => "Cross World Linkshell 1",
        XivChatType.CrossLinkShell2 => "Cross World Linkshell 2",
        XivChatType.CrossLinkShell3 => "Cross World Linkshell 3",
        XivChatType.CrossLinkShell4 => "Cross World Linkshell 4",
        XivChatType.CrossLinkShell5 => "Cross World Linkshell 5",
        XivChatType.CrossLinkShell6 => "Cross World Linkshell 6",
        XivChatType.CrossLinkShell7 => "Cross World Linkshell 7",
        XivChatType.CrossLinkShell8 => "Cross World Linkshell 8",
        _ => string.Empty,
    };

    public static readonly string[] Channels =
    [
        "Alliance", "Custom Emote", "Echo", "Emote", "Free Company",
        "Linkshell 1", "Novice Network", "Party", "PvP Team",
        "Say", "Shout", "Tell", "Tell Out", "Yell",
    ];
}
