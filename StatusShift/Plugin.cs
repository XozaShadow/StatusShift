using System;
using System.Collections.Generic;
using System.Text.Json;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using StatusShift.Windows;

namespace StatusShift;

public sealed partial class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IToastGui Toast { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/statusshift";
    private const string CommandAlias = "/ss";
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public Configuration Configuration { get; }
    public readonly WindowSystem WindowSystem = new("StatusShift");

    private readonly RuleEngine engine;
    private readonly ConfigWindow configWindow;
    private readonly MainWindow mainWindow;
    private readonly SelectorWindow selectorWindow;

    private DateTime lastEval = DateTime.MinValue;
    private DateTime lastApply = DateTime.MinValue;
    private DateTime lastCommandAt = DateTime.MinValue;
    private DateTime pendingSince = DateTime.MinValue;
    private DateTime? pauseUntil;
    private string lastAppliedComment = string.Empty;
    private OnlineStatusAction lastAppliedStatus = OnlineStatusAction.LeaveAlone;
    private string lastAppliedCommand = string.Empty;
    private string lastFingerprint = string.Empty;
    private string? lastMatchedRuleId;
    private string? lastCommandRuleId;
    private string? pendingRuleId;
    private string? lastSelectorKey;
    private bool paused;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        RuleStore.Load(Configuration);
        engine = new RuleEngine(Configuration);

        configWindow = new ConfigWindow(this);
        mainWindow = new MainWindow(this);
        selectorWindow = new SelectorWindow(this);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(selectorWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Status Shift. /ss help",
        });
        CommandManager.AddHandler(CommandAlias, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /statusshift. /ss help",
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        ClientState.TerritoryChanged += OnTerritoryChanged;
        ClientState.Login += OnLogin;
        Framework.Update += OnFrameworkUpdate;

        if (Configuration.OpenUiOnLoad)
            mainWindow.IsOpen = true;
    }

    public void Dispose()
    {
        try { RuleStore.Save(Configuration); } catch { /* ignore */ }
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
    public string ExplainMatch() => engine.Explain();
    public bool IsPaused => paused;
    public string StatusLine()
    {
        if (paused)
        {
            if (pauseUntil is DateTime until)
            {
                var left = Math.Max(0, (int)Math.Ceiling((until - DateTime.Now).TotalSeconds));
                return $"Paused {left}s";
            }
            return "Paused";
        }
        var mode = ApplyModeNames.Label(Configuration.ApplyMode);
        if (Configuration.ApplyMode == ApplyMode.Off)
            return mode;
        var poll = Math.Max(3, Configuration.PollSeconds);
        var next = Math.Max(0, poll - (int)(DateTime.Now - lastEval).TotalSeconds);
        var coolLeft = Math.Max(0, Math.Max(10, Configuration.CooldownSeconds) - (int)(DateTime.Now - lastApply).TotalSeconds);
        return lastApply == DateTime.MinValue
            ? $"{mode} · check {next}s"
            : $"{mode} · check {next}s · cool {coolLeft}s";
    }

    public void RequestEval() => lastEval = DateTime.MinValue;

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

    public bool TryImportOneRule(string text, out string error)
    {
        error = string.Empty;
        text = (text ?? string.Empty).Trim();
        if (text.Length == 0) { error = "Clipboard is empty."; return false; }

        if (text.StartsWith("SS1.", StringComparison.OrdinalIgnoreCase)
            && ChipShare.TryDecode(text, out var decoded, out error) && decoded is not null)
        {
            AddImported(decoded);
            Configuration.Save();
            error = string.Empty;
            return true;
        }

        try
        {
            if (text.StartsWith('['))
            {
                var many = JsonSerializer.Deserialize<List<StatusRule>>(text);
                if (many is null || many.Count == 0) { error = "No rule in clipboard."; return false; }
                foreach (var r in many) AddImported(r);
            }
            else
            {
                var one = JsonSerializer.Deserialize<StatusRule>(text);
                if (one is null) { error = "Clipboard is not a rule."; return false; }
                AddImported(one);
            }
            Configuration.Save();
            return true;
        }
        catch (Exception ex)
        {
            error = string.IsNullOrEmpty(error) ? ex.Message : error;
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
        return ApplyValues(rule, comment, force);
    }

    private bool ApplyValues(StatusRule rule, string comment, bool force)
    {
        var command = rule.Command?.Trim() ?? string.Empty;
        if (!force && comment == lastAppliedComment && rule.OnlineStatus == lastAppliedStatus && command == lastAppliedCommand && lastMatchedRuleId == rule.Id)
            return MaybeRerunCommand(rule, force);

        var ok = true;
        if (rule.ChangeSearchComment && !string.IsNullOrWhiteSpace(comment))
            ok &= ChatSender.TrySendCommand($"/searchcomment {comment}");

        var statusCmd = ChatSender.ToStatusCommand(rule.OnlineStatus);
        if (statusCmd is not null)
            ok &= ChatSender.TrySendCommand(statusCmd);

        if (rule.HasCommand)
            ok &= TrySendRuleCommand(rule, force, allowSelf: true);

        if (ok)
        {
            lastAppliedComment = comment;
            lastAppliedStatus = rule.OnlineStatus;
            lastAppliedCommand = command;
            lastMatchedRuleId = rule.Id;
            lastApply = DateTime.Now;
            Notify(rule.ChangeSearchComment ? $"Applied [{rule.Name}]: {comment}" : $"Applied [{rule.Name}]");
        }
        else Notify($"Failed to apply [{rule.Name}].");

        return ok;
    }

    private bool MaybeRerunCommand(StatusRule rule, bool force)
    {
        if (!rule.HasCommand) return false;
        return TrySendRuleCommand(rule, force, allowSelf: false);
    }

    private bool TrySendRuleCommand(StatusRule rule, bool force, bool allowSelf)
    {
        var command = rule.Command.Trim();
        if (!command.StartsWith('/')) command = "/" + command;

        if (IsSelfCommand(command, out var selfKey))
        {
            if (!allowSelf) return true;
            if (selfKey is "apply" or "now" or "update")
                return true;
            Notify($"Ignored Status Shift command on [{rule.Name}].");
            return true;
        }

        var interval = rule.EffectiveCommandInterval(Configuration.PollSeconds);
        var first = lastCommandRuleId != rule.Id;
        if (!force && !first)
        {
            if (interval <= 0) return true;
            if ((DateTime.Now - lastCommandAt).TotalSeconds < interval) return true;
        }

        var ok = ChatSender.TrySendCommand(command);
        if (ok)
        {
            lastCommandAt = DateTime.Now;
            lastCommandRuleId = rule.Id;
        }
        return ok;
    }

    private static bool IsSelfCommand(string command, out string key)
    {
        key = string.Empty;
        var t = command.Trim();
        if (!t.StartsWith("/ss", StringComparison.OrdinalIgnoreCase)
            && !t.StartsWith("/statusshift", StringComparison.OrdinalIgnoreCase))
            return false;
        var parts = t.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        key = parts.Length > 1 ? parts[1].Trim().ToLowerInvariant() : string.Empty;
        if (key.Contains(' ')) key = key.Split(' ')[0];
        return true;
    }

    private void OnCommand(string command, string args)
    {
        var raw = (args ?? string.Empty).Trim();
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var key = parts.Length == 0 ? string.Empty : parts[0].ToLowerInvariant();
        switch (key)
        {
            case "help": PrintHelp(); break;
            case "config": ToggleConfigUi(); break;
            case "apply":
            case "update": TryApply(force: true); break;
            case "pause":
            {
                paused = true;
                if (parts.Length > 1 && int.TryParse(parts[1], out var secs))
                {
                    if (secs <= 0) pauseUntil = null;
                    else pauseUntil = DateTime.Now.AddSeconds(secs);
                    Notify(secs <= 0 ? "Paused." : $"Paused {secs}s.");
                }
                else
                {
                    pauseUntil = null;
                    Notify("Paused.");
                }
                break;
            }
            case "resume":
                paused = false;
                pauseUntil = null;
                Notify("Resumed.");
                Evaluate(forceNotice: true, fromEvent: true);
                break;
            case "auto":
                Configuration.ApplyMode = ApplyMode.Auto;
                Configuration.Save();
                Notify("Apply mode: Auto");
                Evaluate(forceNotice: true, fromEvent: true);
                break;
            case "confirm":
            case "notify":
            case "notifications":
                Configuration.ApplyMode = ApplyMode.Confirm;
                Configuration.Save();
                Notify("Apply mode: Notifications");
                break;
            case "off":
                Configuration.ApplyMode = ApplyMode.Off;
                Configuration.Save();
                selectorWindow.Hide();
                Notify("Apply mode: Off");
                break;
            case "selector":
                Configuration.ApplyMode = ApplyMode.Selector;
                Configuration.Save();
                Notify("Apply mode: Selector");
                Evaluate(forceNotice: true, fromEvent: true);
                break;
            case "zone":
            {
                var snap = Snapshot();
                Notify($"{snap.WorldName} · {snap.TerritoryName} ({snap.TerritoryId}) · {snap.Housing.Summary} · {snap.JobAbbr}");
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

    private void PrintHelp()
    {
        Notify("/ss — open window");
        Notify("/ss apply | update — apply current match now");
        Notify("/ss now — preview match, do not apply");
        Notify("/ss pause [seconds] — pause all rules; 120 = 2 min");
        Notify("/ss resume — resume rules");
        Notify("/ss auto | notifications | selector | off — set handling");
        Notify("/ss zone — print current place");
        Notify("/ss config — settings");
    }

    private void OnTerritoryChanged(uint _) { lastEval = DateTime.MinValue; Evaluate(fromEvent: true); }
    private void OnLogin() => lastEval = DateTime.MinValue;

    private void OnFrameworkUpdate(IFramework _)
    {
        if (paused && pauseUntil is DateTime until && DateTime.Now >= until)
        {
            paused = false;
            pauseUntil = null;
            Notify("Pause ended.");
            Evaluate(fromEvent: true);
            return;
        }

        if (!Configuration.Enabled || paused || !ClientState.IsLoggedIn) return;
        if (Configuration.ApplyMode == ApplyMode.Off) return;

        var snap = engine.Snapshot();
        var changed = snap.Fingerprint != lastFingerprint;
        if (changed)
        {
            lastFingerprint = snap.Fingerprint;
            Evaluate(fromEvent: true);
            return;
        }

        var interval = Math.Max(3, Configuration.PollSeconds);
        if ((DateTime.Now - lastEval).TotalSeconds < interval) return;
        lastEval = DateTime.Now;
        Evaluate();
    }

    private void Evaluate(bool forceNotice = false, bool fromEvent = false)
    {
        lastEval = DateTime.Now;
        if (Configuration.ApplyMode == ApplyMode.Off) return;

        var rule = engine.FindMatch();
        var id = rule?.Id;
        if (id != pendingRuleId)
        {
            pendingRuleId = id;
            pendingSince = DateTime.Now;
        }

        if (rule is null)
        {
            TryRevert();
            lastCommandRuleId = null;
            lastSelectorKey = null;
            selectorWindow.Hide();
            return;
        }

        if (Configuration.MinMatchSeconds > 0
            && (DateTime.Now - pendingSince).TotalSeconds < Configuration.MinMatchSeconds
            && lastMatchedRuleId != rule.Id)
            return;

        var comment = rule.ChangeSearchComment ? engine.ResolveComment(rule) : string.Empty;
        var command = rule.Command?.Trim() ?? string.Empty;
        var changed = comment != lastAppliedComment || rule.OnlineStatus != lastAppliedStatus || command != lastAppliedCommand || lastMatchedRuleId != rule.Id;
        var dueCommand = rule.HasCommand && rule.RerunCommand && lastCommandRuleId == rule.Id
            && (DateTime.Now - lastCommandAt).TotalSeconds >= rule.EffectiveCommandInterval(Configuration.PollSeconds);

        if (!changed && !dueCommand && !forceNotice) return;

        if (Configuration.ApplyMode == ApplyMode.Selector)
        {
            var matches = engine.FindMatches();
            var key = string.Join("|", matches.ConvertAll(r => r.Id));
            if (forceNotice || key != lastSelectorKey)
            {
                lastSelectorKey = key;
                selectorWindow.Show(matches);
                if (rule.NotifyIfNotApplied)
                    Notify($"Match [{rule.Name}] — pick in selector");
            }
            return;
        }

        if (Configuration.ApplyMode == ApplyMode.Confirm)
        {
            if (changed || forceNotice)
            {
                Notify($"Match [{rule.Name}] — /ss apply");
                if (Configuration.ConfirmPing)
                    GameSounds.Play(Configuration.NotifySound);
            }
            lastMatchedRuleId = rule.Id;
            if (dueCommand && rule.HasCommand)
                TrySendRuleCommand(rule, force: false, allowSelf: false);
            return;
        }

        var newWinner = lastAppliedStatus == OnlineStatusAction.LeaveAlone && lastMatchedRuleId != rule.Id || lastMatchedRuleId != rule.Id;
        if (changed && !newWinner && (DateTime.Now - lastApply).TotalSeconds < Math.Max(5, Configuration.CooldownSeconds)
            && lastApply != DateTime.MinValue)
        {
            if (dueCommand) TrySendRuleCommand(rule, force: false, allowSelf: false);
            return;
        }

        if (changed) TryApply(rule);
        else if (dueCommand) TrySendRuleCommand(rule, force: false, allowSelf: false);
    }

    private void TryRevert()
    {
        if (lastMatchedRuleId is null) return;
        var previous = Configuration.Rules.Find(r => r.Id == lastMatchedRuleId);
        lastMatchedRuleId = null;
        if (previous is null || !previous.RevertWhenFalse) return;
        if ((DateTime.Now - lastApply).TotalSeconds < Math.Max(5, Configuration.CooldownSeconds)
            && Configuration.ApplyMode == ApplyMode.Auto && lastApply != DateTime.MinValue)
            return;

        lastAppliedComment = previous.FallbackComment;
        lastAppliedStatus = previous.FallbackStatus;
        lastAppliedCommand = previous.FallbackCommand ?? string.Empty;
        lastApply = DateTime.Now;

        if (previous.ChangeFallbackComment && !string.IsNullOrWhiteSpace(previous.FallbackComment))
            ChatSender.TrySendCommand($"/searchcomment {previous.FallbackComment}");
        var statusCmd = ChatSender.ToStatusCommand(previous.FallbackStatus);
        if (statusCmd is not null)
            ChatSender.TrySendCommand(statusCmd);
        if (!string.IsNullOrWhiteSpace(previous.FallbackCommand))
        {
            var fb = previous.FallbackCommand.Trim();
            if (!fb.StartsWith('/')) fb = "/" + fb;
            if (!IsSelfCommand(fb, out _))
                ChatSender.TrySendCommand(fb);
        }
        Notify($"Applied [{previous.Name} fallback]");
    }

    private void Notify(string message)
    {
        if (Configuration.ApplyMode == ApplyMode.Off) return;
        if (Configuration.NotifyInChat)
        {
            Chat.Print(new XivChatEntry
            {
                Type = XivChatType.Debug,
                Message = new SeStringBuilder().AddUiForeground("[Status Shift] ", 548).AddText(message).BuiltString,
            });
        }
        if (Configuration.NotifyWithToast)
        {
            try { Toast.ShowNormal(message); }
            catch { /* ignore */ }
        }
    }
}
