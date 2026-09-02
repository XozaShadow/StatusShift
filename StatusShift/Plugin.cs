using System;
using System.Collections.Generic;
using System.Text.Json;
using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using StatusShift.Windows;

namespace StatusShift;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/statusshift";
    private const string CommandAlias = "/ss";
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public Configuration Configuration { get; }
    public readonly WindowSystem WindowSystem = new("StatusShift");

    private readonly RuleEngine engine;
    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;

    private DateTime lastEval = DateTime.MinValue;
    private DateTime lastApply = DateTime.MinValue;
    private string lastAppliedComment = string.Empty;
    private OnlineStatusAction lastAppliedStatus = OnlineStatusAction.LeaveAlone;
    private string? lastMatchedRuleId;
    private bool paused;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        engine = new RuleEngine(Configuration);

        configWindow = new ConfigWindow(this);
        mainWindow = new MainWindow(this);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open Status Shift. /ss apply | pause | resume | now | config | zone",
        });
        CommandManager.AddHandler(CommandAlias, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /statusshift",
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        ClientState.TerritoryChanged += OnTerritoryChanged;
        ClientState.Login += OnLogin;
        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        ClientState.TerritoryChanged -= OnTerritoryChanged;
        ClientState.Login -= OnLogin;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        WindowSystem.RemoveAllWindows();
        configWindow.Dispose();
        mainWindow.Dispose();
        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(CommandAlias);
    }

    public void ToggleConfigUi() => configWindow.Toggle();
    public void ToggleMainUi() => mainWindow.Toggle();
    public StatusRule? CurrentRule() => engine.FindMatch();
    public string PreviewComment(StatusRule rule) => engine.ResolveComment(rule);
    public GameSnapshot Snapshot() => engine.Snapshot();

    public string ExportRulesJson() => JsonSerializer.Serialize(Configuration.Rules, JsonOpts);

    public string ExportRuleJson(StatusRule rule) => JsonSerializer.Serialize(rule, JsonOpts);

    public bool TryImportRulesJson(string json, out string error)
    {
        error = string.Empty;
        try
        {
            var rules = JsonSerializer.Deserialize<List<StatusRule>>(json);
            if (rules is null) { error = "Empty import."; return false; }
            Configuration.Rules = rules;
            Configuration.Save();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public bool TryImportOneRule(string json, out string error)
    {
        error = string.Empty;
        json = json.Trim();
        try
        {
            if (json.StartsWith('['))
            {
                var many = JsonSerializer.Deserialize<List<StatusRule>>(json);
                if (many is null || many.Count == 0) { error = "No rule in clipboard."; return false; }
                foreach (var r in many) AddImported(r);
            }
            else
            {
                var one = JsonSerializer.Deserialize<StatusRule>(json);
                if (one is null) { error = "Clipboard is not a rule."; return false; }
                AddImported(one);
            }
            Configuration.Save();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private void AddImported(StatusRule rule)
    {
        rule.Id = Guid.NewGuid().ToString("N");
        rule.Name = string.IsNullOrWhiteSpace(rule.Name) ? "Imported rule" : rule.Name + " (copy)";
        Configuration.Rules.Add(rule);
    }

    public bool TryApply(StatusRule? rule = null, bool force = false)
    {
        rule ??= engine.FindMatch();
        if (rule is null)
        {
            Notify("No matching rule.");
            return false;
        }

        var comment = rule.ChangeSearchComment ? engine.ResolveComment(rule) : string.Empty;
        return ApplyValues(rule.Name, comment, rule.ChangeSearchComment, rule.OnlineStatus, force, rule.Id);
    }

    private bool ApplyValues(string name, string comment, bool writeComment, OnlineStatusAction status, bool force, string? ruleId)
    {
        if (!force && comment == lastAppliedComment && status == lastAppliedStatus)
            return false;

        var ok = true;
        if (writeComment && !string.IsNullOrWhiteSpace(comment))
            ok &= ChatSender.TrySendCommand($"/searchcomment {comment}");

        var statusCmd = ChatSender.ToStatusCommand(status);
        if (statusCmd is not null)
            ok &= ChatSender.TrySendCommand(statusCmd);

        if (ok)
        {
            lastAppliedComment = comment;
            lastAppliedStatus = status;
            lastMatchedRuleId = ruleId;
            lastApply = DateTime.Now;
            Notify(writeComment ? $"Applied [{name}]: {comment}" : $"Applied [{name}]");
        }
        else Notify($"Failed to apply [{name}].");

        return ok;
    }

    private void OnCommand(string command, string args)
    {
        var key = (args ?? string.Empty).Trim().ToLowerInvariant();
        switch (key)
        {
            case "config": ToggleConfigUi(); break;
            case "apply": TryApply(force: true); break;
            case "pause": paused = true; Notify("Paused."); break;
            case "resume": paused = false; Notify("Resumed."); Evaluate(forceNotice: true); break;
            case "zone":
            {
                var snap = Snapshot();
                Notify($"{snap.TerritoryId} {snap.TerritoryName} i{snap.Instance} / {snap.RegionName} | {snap.JobAbbr} | {snap.WorldName}");
                break;
            }
            case "now":
            {
                var rule = engine.FindMatch();
                Notify(rule is null ? "No matching rule." : $"Would apply [{rule.Name}] P{rule.Priority}");
                break;
            }
            default: ToggleMainUi(); break;
        }
    }

    private void OnTerritoryChanged(uint _) { lastEval = DateTime.MinValue; Evaluate(); }
    private void OnLogin() => lastEval = DateTime.MinValue;

    private void OnFrameworkUpdate(IFramework _)
    {
        if (!Configuration.Enabled || paused || !ClientState.IsLoggedIn) return;
        var interval = Math.Max(3, Configuration.PollSeconds);
        if ((DateTime.Now - lastEval).TotalSeconds < interval) return;
        lastEval = DateTime.Now;
        Evaluate();
    }

    private void Evaluate(bool forceNotice = false)
    {
        var rule = engine.FindMatch();
        if (rule is null)
        {
            TryRevert();
            return;
        }

        var comment = rule.ChangeSearchComment ? engine.ResolveComment(rule) : string.Empty;
        var changed = comment != lastAppliedComment || rule.OnlineStatus != lastAppliedStatus;
        if (!changed && !forceNotice) return;

        if (Configuration.ApplyMode == ApplyMode.Confirm)
        {
            Notify($"Match [{rule.Name}] — /ss apply");
            lastMatchedRuleId = rule.Id;
            return;
        }

        if ((DateTime.Now - lastApply).TotalSeconds < Math.Max(10, Configuration.CooldownSeconds))
            return;

        TryApply(rule);
    }

    private void TryRevert()
    {
        if (lastMatchedRuleId is null) return;
        var previous = Configuration.Rules.Find(r => r.Id == lastMatchedRuleId);
        lastMatchedRuleId = null;
        if (previous is null || !previous.RevertWhenFalse) return;
        if ((DateTime.Now - lastApply).TotalSeconds < Math.Max(10, Configuration.CooldownSeconds)
            && Configuration.ApplyMode == ApplyMode.Auto)
            return;

        ApplyValues(
            previous.Name + " fallback",
            previous.FallbackComment,
            previous.ChangeFallbackComment,
            previous.FallbackStatus,
            force: true,
            ruleId: null);
    }

    private void Notify(string message)
    {
        if (!Configuration.NotifyInChat) return;
        Chat.Print(new XivChatEntry
        {
            Type = XivChatType.Debug,
            Message = new SeStringBuilder().AddUiForeground("[Status Shift] ", 548).AddText(message).BuiltString,
        });
    }
}
