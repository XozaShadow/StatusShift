# StatusShift

StatusShift is a Dalamud plugin for Final Fantasy XIV. It changes your **online status** and optional **search comment** from simple rules: where you are, what you are doing, which job you are on, and when the clock says so.

Highest priority matching rule wins. Everything else stays put until a rule says otherwise.

## What it is for

Typical uses:

- Busy while in a duty, Online when you leave
- Role-playing on a schedule (weekend evenings, venue hours)
- A different status on a specific world or in a house / apartment
- A fallback rule so you are never left on the wrong status

Rules can also run a slash command when they match, and can restore a previous status when they stop matching.

## Install

Install from the **Dalamud plugin installer** (`/xlplugins`) when StatusShift is listed there.
`https://raw.githubusercontent.com/XozaShadow/StatusShift/main/repo.json`

## Quick start

1. Open **Status Shift** with `/ss` or from the plugin installer.
2. Add a rule. Give it a name and a priority (higher number wins).
3. Set **During schedule** if it should only run at certain times.
4. Add **If these conditions** with AND / OR chips (world, zone, job, activity, and so on).
5. Set **Then** to the online status you want. Optionally add a search comment or a slash command.
6. Choose what happens when the rule stops matching: **revert** to another status, or **keep** what this rule set.
7. Turn the rule **On**.

The header shows the current match. Click it to edit that rule. Use **Check Now** to apply immediately.

## Handling modes

Set in Settings, or with `/ss`:

| Mode | What it does |
| --- | --- |
| Notifications | Tells you a rule matched. Apply with `/ss apply` or `/ss update`. |
| Selector | Shows matching rules. Click one to apply. |
| Auto | Applies the highest matching rule after the cooldown. |
| Off | Does not check or notify. |

## Commands

| Command | Action |
| --- | --- |
| `/ss` or `/statusshift` | Open the main window |
| `/ss apply` or `/ss update` | Apply the current match |
| `/ss now` | Preview the match, do not apply |
| `/ss pause [seconds]` | Pause rules (`120` = two minutes) |
| `/ss resume` | Resume |
| `/ss auto` `/ss notifications` `/ss selector` `/ss off` | Set handling mode |
| `/ss zone` | Print current place and job |
| `/ss config` | Open Settings |
| `/ss help` | Command list |

## Rules in short

- **Priority** — higher number wins when more than one rule matches.
- **Category** — optional folder in the left list. A character filter also appears under Characters.
- **Schedule** — Always, Daily, Weekly, One Time, or Custom. Times are 24-hour `HH:mm`. Dates are `YYYY-MM-DD`.
- **Conditions** — AND chips must all match. OR chips need one match. Empty lists mean “any.”
- **Then** — online status (or leave it alone), optional slash command, optional search comment (60 characters, same specials as the in-game search info).
- **Comment templates** — titled 60-character snippets. A rule can type a comment or pick a template. Edit a template once and every rule using it updates.
- **Repeat command** — run the slash command once, or every N seconds (`0` uses the Settings check interval).
- **When it ends** — revert to another status / comment / command, or keep what this rule set.

Search comment tokens: `{zone}` `{region}` `{job}` `{world}` `{home}` `{ward}` `{plot}` `{time}`.

Command tokens: `{teller}` `{targeter}` `{target}` `{zone}` `{job}` `{world}` `{home}` `{dc}` `{ward}` `{plot}` `{time}`.

Share one rule from the editor as JSON or as an `SS1.` share code. Paste that with **Import Rule**. Settings can copy or replace the full ruleset (Shift required to replace, archive, or wipe).

**Selector:** matching rules appear even if they are off. Checkbox turns a rule on or off. Click the name to apply. Handling mode can be changed from that window.

**Test** buttons on a rule run that section now (ignores timers). Full / Then tests revert after 5 seconds.

## Samples

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
## Settings

- Skip checks while in combat, dead, in duty, in a cutscene, occupied, between areas, targeting, targeted, or emoting.
- Check interval and minimum match time.
- Auto cooldown.
- Chat, toast, and built-in game sound notifications.
- Live analysis of the current job, place, and activity flags, plus which rules match.

## Build

Windows, .NET 10, and Dalamud.

```
dotnet build StatusShift.slnx -c Release
```
