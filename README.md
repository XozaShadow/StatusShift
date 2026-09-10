# StatusShift

StatusShift is a Dalamud plugin for Final Fantasy XIV. Rules change your **online status**, optional **search comment**, and optional **slash commands** from where you are, what you are doing, which job you are on, who you are playing, and what the clock says.

Highest priority matching rule wins. Everything else stays put until a rule says otherwise. Nothing runs on the title screen or character select.

**1.0.0.0** is the first public release. Older `0.1.x` GitHub tags are kept as prereleases for history.

## What it is for

- Busy in a duty, Online when you leave
- Role-playing on a schedule (weekend evenings, venue hours)
- A different status on a specific world, data center, house, apartment, or FC apartment
- Search comments that flip between “player” and “roleplay” without retyping
- A fallback rule so you are never left on the wrong status
- Optional slash commands when a rule matches, with tokens like `{teller}` and `{target}`

## Install

1. In game, `/xlsettings` → **Experimental**.
2. Under **Custom Plugin Repositories**, add:

```
https://raw.githubusercontent.com/XozaShadow/StatusShift/main/repo.json
```

3. Save, then `/xlplugins`, search **Status Shift**, install.
4. `/ss` to open.

Also listed in the Dalamud plugin installer when StatusShift is accepted there.

Enable **Get plugin testing versions** in Dalamud only if you want test builds. Stable users can ignore that.

## Quick start

1. Open **Status Shift** with `/ss`.
2. **Add Rule**. Name it. Set **priority** (higher number wins when two rules match).
3. Optional: **Category**, **Character** (`First Last` or `First Last@World`), **Notes**.
4. **During schedule** if it should only run at certain times. Leave Always if not.
5. **If these conditions** — add AND / OR / NOT chips (world, zone, job, state, housing, and so on). Empty lists mean “any.”
6. **Then set / run / update** — online status, optional command, optional 60-character search comment (typed or a template).
7. **When this rule stops matching** — **Revert** to the values below, or **Keep** what this rule set.
8. Turn the rule **On**.

The header shows the current match. Click it to edit. **Check Now** applies immediately (when Handling is not Off).

## Handling modes

Set in Settings, on the Selector window, or with `/ss`.

| Mode | What it does |
| --- | --- |
| Notifications | Tells you a rule matched (chat / toast / sound). Apply with `/ss apply` or `/ss update`. |
| Selector | Popup of matching rules. Checkbox enables or disables. Click the **name** to apply. |
| Auto | Applies the highest matching rule after the cooldown. |
| Off | No checks, no popups, no notifications. |

## Commands

| Command | Action |
| --- | --- |
| `/ss` or `/statusshift` | Open the main window |
| `/ss apply` or `/ss update` | Apply the current match |
| `/ss now` | Preview the match, do not apply |
| `/ss pause [seconds]` | Pause rules (`120` = two minutes; omit seconds = until resume) |
| `/ss resume` | Resume |
| `/ss auto` `/ss notifications` `/ss selector` `/ss off` | Set handling mode |
| `/ss zone` | Print current world, zone, housing, job |
| `/ss config` | Open Settings |
| `/ss help` | Command list |

A leading `/` on a rule’s command field is optional. `busy on` and `/busy on` both work.

## Window layout

- **Left:** All, Ungrouped, **Categories**, **Characters**.
- **Middle:** rule list (priority, on, name, status, notes). Filter box, Add Rule, Import Rule. Move up / down keeps priority numbers.
- **Editor:** under the list. Header: `EDITING: P##: Name | Test Duplicate Close Delete | Copy Share: Code JSON`.
- Delete, replace-all, archive, and wipe need **Shift**.

## Rules

- **Priority** — higher number wins.
- **On** — disabled rules never apply. Selector still lists them if they *would* match, so you can turn them on there.
- **Category** — folder in the left list. Assigning a **character** also lists the rule under Characters (two listings is intended).
- **Character** — blank = all characters on this client. `First Last` or `First Last@World`.
- **Notify if this rule matches but is not applied** — Chat and/or Sound (per-rule sound slider + Test).

### Schedule

Always, Daily, Weekly, One Time, or Custom. Days: `S M T W T F S`. Times are 24-hour `HH:mm`. Dates are `YYYY-MM-DD`. All Day can sit at the end of the day row.

### Conditions (AND / OR / NOT)

AND chips must all match. OR needs one match. NOT chips must not match. Empty group is ignored.

Searchable pickers for longer lists (type 3+ letters on very long lists).

| Condition | Notes |
| --- | --- |
| Accessory | Fashion accessory in use |
| Chat | Channel (say, shout, LS1–8, CWLS1–8, …) |
| Contains / Regex | Text match |
| Data center | Current DC |
| Duty | Duty name |
| Emote | Current emote |
| Housing | Property / Apartment / FC Apartment — zone, ward, plot, room, subdivision (same idea as Lifestream) |
| Job | Full names, abbreviations, roles (tank, healer, melee DPS, …), DoW / DoM / DoL / DoH |
| Mount | Mount in use |
| Nearby player | Name within range (Settings) |
| Region | Region name |
| State | Combat, mounted, walking, in duty, sitting, … |
| Status | Current **online status**. If Status is a condition, Then Set status is locked to **Leave alone** |
| Tell from | Last tell sender |
| World | Current world |
| Zone | Zone / territory name |
| Zone group | Inn, house, apartment, residential area, duty, aquatic, city, and similar |

### Then set / run / update

- Online status, or **Leave alone**.
- Command / macro. Wait N seconds before first send. Optional **repeat every N seconds** (`0` = Settings check interval).
- Search comment: 60 characters, same specials as the in-game search info. **Type comment** or **Use template**.

If **Status** is used as a condition, Then Set cannot also change status.

### Comment templates

Titled 60-character snippets in Settings (chips: right-click edit, copy last applied, delete). A rule can type a one-off or pick a template. Edit the template once; every rule using it updates on the next apply. **Save as template** stores the box (or last applied comment).

### When the rule stops matching

- **Revert** to the fallback status / comment / command below, or
- **Keep** what this rule set until another rule changes it.

Default is revert.

### Test buttons

Ignore timers. Hover each button for the exact path.

| Where | What it does |
| --- | --- |
| Editor header | Character → schedule → conditions → Then. Reverts after 5 seconds |
| During schedule | Starts at schedule |
| If these conditions | Conditions onward |
| Then set | Fires Then, waits 5 seconds, then the stop path |
| When this rule stops matching | Runs revert / keep immediately |

## Sharing

- **Code** — compact `SS1.` share string (one rule).
- **JSON** — full rule JSON.

**Import Rule** pastes either. Settings can copy the whole ruleset. Replace / archive / wipe need Shift. **Archive Current & Wipe** copies `rules.json` then starts empty. Save path is shown on one line.

## Selector

Shows every rule that matches **right now**, on or off.

- **Active** (enabled) on top, **Inactive** below.
- Checkbox = enable / disable only.
- Click the **title** = apply that rule.
- Handling mode can be changed from this window.

## Settings

- Skip checks while in combat, dead, in duty, cutscene, occupied, between areas, targeting, targeted, or emoting.
- Check interval, minimum match time, Auto cooldown.
- Chat, toast, and game-sound notifications (global sound slider + Test).
- Open main window on load; live snapshot at the top of the main window.
- Comment templates.
- Analysis: live job, world, DC, zone, housing, mount/emote, activity flags (blue = true), then which rules match.
- Full ruleset backup / import / archive.

## Tokens

Search comment: `{zone}` `{region}` `{job}` `{world}` `{home}` `{ward}` `{plot}` `{time}`.

Commands also: `{teller}` `{targeter}` `{target}` `{dc}`.

## Samples

Import with **Import Rule**. Review, then turn **On**.

### Sets Mentor when you're running content.

```
SS1.H4sIAAAAAAACCq1US3PaMBD-Kx2fOWAI4XFzeEzchIYJpByaHGRpCZoIiZElZjwZ_ntXkm2Mm_bUi6z9dlf7-tafUcqiSTQcZjd9OhoNe7eDm5tBPB5CRsloB_3bmA1YFnWiH-QAaLkEaZR2sjKQI4DXhRIMNN5n1hQoT_dEE2pAL7gwXoHgXJJMAAYz2kInWmmuNEfzSewd5DusgWi6n6rDAWNEkx0RORq2UPfUkxRcwtoQYzGDYSd6hhNos92DXDinKgYKIiP0o7KsI1WKdqw_8OjVzru3iT9jf3bD-c19hn13jsb-3vPnwCO9BjL3yKBxjl-laxLGIJKFip5BW1kjZTalnErs4YmINVAlGZbRxabQPTArsNLPaKkYeDARYkaKqngsWpt7ZbH941JacmlNsJ1LFnRxzwsNFb6BQX69uZsB7xhNpBUiAHOXoRPP_lVPAmfs70ti6N6_8qgoMVxJl-EDdz4I_iTCQlVxzhlICkGJo5nx3GhOyxlviQ4-K6FMKO-ImYSxuA7YjPETz30I3zDMZ6u0YE3SecAxt0wyoQadDK_kDWgkodJFytqI85oqaQiXpeq7ymozvCdZpkvJh6l1lwam0m9E2b0NP1y30wF1O10HOf0o6vHfk7zNCAe1d6uirt_BBygua5hINt3zo8ulmkEcX4aQyhXm4gwfSQaigZwx9Sf9L987ZSW7K8pAlX8TPXf-p9-W4NDk-0JpZ7RAPejGA1-qv34pdWuWEXNVdgm5uvG3VhX-Vq_gDAQp6v2Lu0GBVNzA4SiIKTnd-n38XVnvPUbjuyLd4Tc5HgWHy6yDCsdtWlBiGc_c6l-ha9fD6hcXGBLIgFFecsjDPxAJzbi57Iyn1CO8E1rx7vwbUINEwhIGAAA
```

### Sets Role-play when you're walking and in city areas.

```
SS1.H4sIAAAAAAACCnVUS28iMQz-K9WcOZRH1ZbbiIfKFrYVsOWw6sGTGIgICcokSKjqf18nmRdT9jTjz_5i57Odr2TGk2HC-uyh_wjdQcZhwHr49JxlvP_w2M8G3f7g6T7pJL_hiBS51BJPEi5C7TyoLeaE0u9US46mEUHYaA8GmEUzFdIGJ4ETBZlEymqNw07yboQ2wl6SYS8Q1A5XCIbtR_p4RGWT4RZkToEt1B_1pqRQuLJgHVUx6CRLPKOxmz2qqSdVXLJkBuxQhnbLVKWjnewH7tN5AxSPxhKNUxVS0Ap7pui2Z5ArZFpxyndP5bM9cieppq9koTkGMJVyTEoVUlB1xr5oR0I9F9ZCKGdj7ETx6Ov2gtFw0RmU5O-n_7MYiMlQOSkjMPEVevM7nBpa5oPD_wIs24dT5pqBFVr5Cl-F5xD4AdJheeNccFQMo5M0HIvcGsEKeTZgIuddahuvd6JKon5eAZdxcRZ5SBEEo3o22kjeHI8A-GErikyZJZIVpb1GQ-OizWXG24hnjbSyIFTh-qWzKoz-0ywzhRXSVL5awJkaOz-MUb21OF7L6YFKTq-gYIeqfy-QtwfCQ-0lKEcsLMwrXq53JlV8tBcnX07Zhm637sMG5CHu3hwylA3km6p_M21uv6bW3ajJ67sa_e7cII38ajbjA3AzlKaTVq4ZG5GbwTOlrkK9fTOwmDorQN6lBuGK9cN584gVKGYdmOuL1KhXjl6yUrrPao_HSD1pLnHxHKzxSN2yxV603or_O6u3g5KJ7WW2pW96OkmB9cBEF82MbUGp4yKT2EJX2hW7WI1ZnCjK8ifHPD54tBRc2HrvwlzOcQfsUm7iPz3MPo8JBgAA
```

### Sets just online status when you're not doing anything else. Good for a fallback.

```
SS1.H4sIAAAAAAACCnVTS2_bMAz-K4XPOcTpsjS5eXmgWZu1aLLlsO5AS3QjVJECWQpgFPvvox5xPA-7SORHUiQ_kR_ZmmezrMrLyZDhNB9Nhp9Yjnejajq-hQm7zacllJANsm9wRPJcYAVO2putBetqj2uLNRlIXGnJ0ZD8BWrBCJgfwACzaFZC2mAhcKmglEhZrXE4yJ6N0EbYJpsNQ4B6wy2CYYe5Ph5R2WxWgazJsYf6p56UFApTKbN8kL3gGY3dH1CtfFAbS5osgb13XGOqi6Gf7B88e3XL4ecinHk4h_G88dfk1p930yCPwjkOyKiDLAMy7pzTV-VZohygeGzpBY1TLZKqSfpaEYlnkFtkWvE6MLZlB-ROUqsf2UZzDGAh5QKaC8PUtLH32hH_06RthHI2-i4Vj7Z8FJSOid6gJD9_ecliCMxmykkZgaWv0Ku_w6thDLxzkDdg2SG88qgZWKGVr_BB-BgCf4B0eOm4FhwVw2ikr1mI2hrB0ifvwcSYZ6ltbO9ElcRv8Qy4kouzqEOKQBjVs9dG8u7UBcDPcCqyYJaCrLjoOzQ0hdo0a95HfNRcKwtCJdNXXbZuJBdlaZIW0rS2K4FrtXB-xiN7O3H8m04PtHR6BgV7b__vHur-QHiov1uXyQ1L-IBNZw8LxecHcUq1PJmOQuvb0VKeBUpoulOW1mCHx5MEmz6utyP_N7bDTclE1awruovTSQq8dhRN1JTtQYXjopTYQ7fapWFpeYgtU5bvNdZx0enXuLDXwQjEPeIbsOYyKn8ARtvNDQEFAAA
```

## Notes

- Does nothing until a character is in the world. Logout does not send revert commands.
- If two rules fight, raise the one you want, or add a NOT chip. Rare “stuck after interrupt” cases usually clear with a clearer fallback or `/ss apply`.
- FC apartments on a plot use **Zone Ward ## Plot ## Room ##**. Standalone wing apartments do not use plot.

## Build

Windows, .NET 10, Dalamud API 15.

```
dotnet build StatusShift.slnx -c Release
```

Source: https://github.com/XozaShadow/StatusShift
