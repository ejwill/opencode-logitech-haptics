# OpenCode Companion User Guide

OpenCode Companion is an independent Logitech integration for OpenCode. It sends OpenCode notifications to a Logitech MX Master 4 / MX 4 and represents them as haptic feedback.

It is not affiliated with or endorsed by the OpenCode project.

## What the plugin provides

The plugin currently provides these event actions:

| Action | Meaning | Icon meaning |
| --- | --- | --- |
| Turn Complete Haptics | OpenCode finished a turn | Green check |
| Permission Request Haptics | OpenCode needs approval | Warning triangle |
| Turn Error Haptics | OpenCode reported a failure | Red error mark |
| Question Haptics | OpenCode is waiting for an answer | Question mark |
| Plan Ready Haptics | A plan is ready for review | Plan with check |
| Test Haptic Feedback | Sends a test vibration | Haptic waves |

The event actions are quick enable/disable controls. **Haptic Preferences** is the current configuration action for saved notification and waveform preferences.

## OpenCode controls

OpenCode Companion also provides a planned control surface for the OpenCode workflow itself. OpenCode is available as a terminal interface, desktop app, and IDE extension, so these controls are implemented through an OpenCode control adapter rather than by assuming a specific window or terminal shortcut.

The initial control set is intentionally small:

| Action | Purpose | Safety boundary |
| --- | --- | --- |
| Switch Agent | Cycle through available primary agents | Uses OpenCode's `agent_cycle` TUI command; does not execute a prompt or change permissions |
| Switch Thinking Level | Cycle through variants available for the current model | Uses OpenCode's `variant_cycle` TUI command; does not assume `low`/`medium`/`high` exist |
| Stop Current Turn | Abort the active OpenCode turn | Requires an identified active session; does not delete or revert work |
| Open Session Switcher | Open OpenCode's session selector | The user chooses the session interactively |
| Open Model Selector | Open the current model selector | The user chooses the model interactively |
| Open OpenCode | Launch or focus the configured OpenCode desktop/terminal entrypoint | Does not submit prompts or run commands |

The first two actions use OpenCode's supported TUI command endpoint. The next three use OpenCode's UI or session controls. The launch action is a platform adapter and may require a configured executable or desktop application path.

The current implementation includes **Switch Agent**, **Switch Thinking Level**, **Stop Current Turn**, **Open Session Switcher**, and **Open Model Selector**. When the OpenCode host supplies its `serverUrl`, the plugin reports it to the Logitech bridge during startup and the control actions use that URL automatically. If no startup URL is available, they use `OPENCODE_SERVER_URL` when set, or default to `http://127.0.0.1:4096`.

The initial release deliberately excludes prompt submission, arbitrary slash commands, shell execution, permission approval, session deletion, and message reversion. Those operations can mutate state or execute work and need a stronger active-session, confirmation, and authentication model.

OpenCode agents and model variants are runtime-dependent. The control adapter must query the active OpenCode runtime and display only available choices. It must not hardcode a universal list of agents or thinking levels.

## Recommended Logi Options+ layout

Logi Options+ folders are created and arranged by the user. The plugin supplies the actions; it does not create or manage the folder itself.

Create a folder named **OpenCode Companion**, then add these actions:

```text
OpenCode Companion
├── Turn Complete Haptics
├── Turn Complete Preferences
├── Permission Request Haptics
├── Permission Preferences
├── Turn Error Haptics
├── Turn Error Preferences
├── Question Haptics
├── Question Preferences
├── Plan Ready Haptics
├── Plan Ready Preferences
├── Haptic Preferences
├── Test Haptic Feedback
├── Switch Agent
├── Switch Thinking Level
├── Stop Current Turn
├── Open Session Switcher
└── Open Model Selector
```

The first five actions can be assigned to buttons or kept in the folder as quick controls. Use **Test Haptic Feedback** before troubleshooting OpenCode event delivery.

## Configure Haptic Preferences

1. Add **Haptic Preferences** from **OpenCode Companion → Haptic Preferences** to a device button or layout position.
2. Open its Action Editor.
3. Enable or disable the five notification types.
4. Choose a waveform for each notification.
5. Optionally choose a global intensity profile.
6. Save the action.

The current intensity profiles are:

| Profile | Purpose |
| --- | --- |
| Subtle | Less distracting patterns for frequent notifications |
| Normal | Balanced default behavior |
| Strong | More noticeable patterns for important notifications |

The Logitech haptics API exposes named waveforms rather than a numeric amplitude control. Intensity therefore selects a curated set of waveform patterns; it does not directly set the motor strength to an exact percentage.

## Waveform choices

The available waveform names are:

```text
sharp_state_change  damp_state_change  sharp_collision
damp_collision      subtle_collision   happy_alert
angry_alert         completed          square
wave                firework           mad
knock               jingle             ringing
```

The exact feel depends on the MX Master 4 hardware and the selected waveform mapping. Logitech's package mapping provides a `DEFAULT` mapping and device-specific mappings where supported.

## OpenCode configuration

The OpenCode adapter can be configured with JSON or JSONC:

```json
{
  "enabled": true,
  "endpoint": "http://127.0.0.1:17844/haptic",
  "intensity": "normal",
  "notifications": {
    "completion": true,
    "permission": true,
    "question": true,
    "error": true,
    "planReady": true
  },
  "waveforms": {
    "complete": "completed",
    "permission": "knock",
    "error": "angry_alert",
    "question": "ringing",
    "plan_exit": "happy_alert"
  }
}
```

Valid intensity values are `subtle`, `normal`, and `strong`. An explicit `waveforms` entry overrides the selected OpenCode intensity profile.

### Installing with the OpenCode v2 CLI

On an OpenCode v2 (`opencode2`) setup, install the adapter without editing configuration by hand:

```bash
opencode2 plugin add opencode-logi-companion-v2
opencode2 plugin list        # confirm it loaded
```

Watched config directories reload automatically, so edits to `opencode-logi-companion.jsonc` apply without restarting. Restart the service after changing an installed package version:

```bash
opencode2 service restart
```

## Configuration precedence

The effective waveform is resolved in this order:

1. Saved per-event waveform override in Logi Options+.
2. Saved Logitech intensity profile.
3. OpenCode per-event `waveforms` override.
4. OpenCode `intensity` profile.
5. Packaged Logitech default mapping.

This lets advanced OpenCode users edit JSONC while still allowing device-local preferences in Logi Options+.

## Test the bridge manually

On OpenCode v2, run the built-in diagnostic command (it bypasses notification toggles and always fires):

```text
/haptic-test
```

The command logs `haptic-test sent` on success. Alternatively, with the Logitech plugin loaded, send a test event directly:

```bash
curl -i -X POST http://127.0.0.1:17844/haptic \
  -H "Content-Type: application/json" \
  --data '{"source":"opencode","event":"test","message":"Manual test"}'
```

The expected response is `HTTP/1.1 202 Accepted`. If the response succeeds but there is no vibration, check that the MX Master 4 is connected and that haptics are enabled in Logi Options+.

## Current and planned configuration experience

The plugin has one bulk **Haptic Preferences** editor containing all notification toggles, intensity profiles, and waveform selectors. Each notification also has a matching **Preferences** action with a compact editor containing only that event's enabled state, intensity, and waveform. Use the bulk editor for an overview or the per-event editor when tuning one notification from its own Logi+ action.

The OpenCode controls use a separate adapter seam from the haptic bridge. Haptics receive notification events; controls send explicitly scoped UI or session commands. This keeps a control failure from affecting notification delivery.
