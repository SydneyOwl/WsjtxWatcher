# WsjtxWatcher

## Introduction

`WsjtxWatcher` is an Android companion app for `WSJT-X` and `JTDX`.

It can receive live WSJT-X traffic in two ways:

- direct UDP from the same local network
- a relay-backed `Third-party data source`

The app shows decoded traffic in real time and can notify you when messages match your watched callsign patterns, selected DXCC entities, or other configured conditions. It is especially useful for remote monitoring and VHF / DX style watching.

When the app is in the foreground, you can watch live traffic directly. When it is in the background or the phone is locked, it can still raise notifications based on your configured rules.

Currently, the app supports Simplified Chinese and English.

<img src="./md_assets/page3.png" style="zoom: 60%;" />

## Supported Android Versions

- Android 8.0 and above

## Data Source Modes

### UDP

Use this mode when your phone and the WSJT-X / JTDX computer are on the same LAN.

In this mode:

- the app listens on the phone's local IP and UDP port
- WSJT-X or JTDX sends UDP packets directly to the phone

### Third-party data source

See: [wsjtx-relay](https://github.com/SydneyOwl/wsjtx-relay)

Use this mode when the source is remote or when direct LAN UDP is not possible.

In this mode:

- a remote `wsjtx-relay-server` accepts live relay connections
- a `wsjtx-relay-client` runs near WSJT-X / JTDX and uploads live events
- `WsjtxWatcher` connects to the relay server as a watcher
- the app selects one available relay source and consumes its live events

This mode is live-only. It does not replay old decode history after reconnect.

## Quick Start

### Option A: Local UDP

1. Open `Settings`.
2. Keep `Data source` set to `UDP`.
3. Enter your own callsign and Maidenhead locator, then adjust any notification settings you want.
4. Return to the main screen and tap `Start Service`.
5. In WSJT-X or JTDX, send UDP output to the app's displayed LAN IP and server port.

<img src="./md_assets/page4.png" style="zoom: 67%;" />

Make sure the computer running WSJT-X / JTDX and the phone are on the same local network.

### Option B: Third-party data source

1. Deploy and start `wsjtx-relay-server`.
2. Run `wsjtx-relay-client` on the station side so it can receive WSJT-X UDP locally and push events to the relay.
3. Open `Settings` in `WsjtxWatcher`.
4. Change `Data source` to `Third-party data source`.
5. Fill in:
   - `Server URL`
   - `Shared Secret`
   - `Tenant ID`
6. Save settings.
7. Return to the main screen and tap `Start Service`.
8. Open `Select source` and choose the relay source you want to monitor.

If this is the first successful connection, the app will pair with the server certificate automatically by storing its fingerprint. If the server certificate changes later, use `Re-pair server` and connect again.

## Settings Guide

### Common Settings

- `Data source`
  - selects either `UDP` or `Third-party data source`
- `Callsign`
  - your own station callsign
  - used to highlight messages containing your callsign
  - also used as the default watched callsign pattern when no custom pattern list exists
- `Location`
  - your 4-character Maidenhead grid
  - used for distance calculations when possible
- `Language`
  - switches between Simplified Chinese and English
  - the app saves settings, stops the background service, and exits so the new language can take effect after restart

### UDP Settings

- `LAN IP` / `Server Port`
  - the local UDP address that WSJT-X or JTDX should send messages to

### Third-party Data Source Settings

- `Server URL`
  - relay server base URL
  - use a value such as `wss://example.com:8443`
- `Shared Secret`
  - relay authentication secret
  - must match the relay client and relay server
- `Tenant ID`
  - the shared private ID used by both the relay client and the watcher
  - think of it as the private relay room both sides must join
  - use a long random value, not an easy name such as `home` or `test`
- `Relay status`
  - current connection or reconnect state of the relay watcher
- `Test connection`
  - validates URL, certificate pairing, and shared-secret authentication
- `Select source`
  - opens the relay source picker
  - the selected source becomes the preferred source for future reconnects
- `Refresh source list`
  - reconnects to the relay and reloads the available source catalog
- `Re-pair server`
  - clears the stored trusted fingerprint
  - use this if the relay server certificate has changed

### Notification Triggers

- `When the message matches specified callsigns`
  - triggers notification or vibration when the decoded `de` / `dx` callsign matches one of your watched regex patterns
- `Specified callsign match target`
  - chooses whether watched callsign matching checks the `transmitter`, the `receiver`, or `both`
- `Regex`
  - opens the watched callsign regex editor
- `When a WSJT-X message is received`
  - triggers notification or vibration for every decoded message
  - mainly useful for VHF / DX style monitoring
- `When selected DXCC appears`
  - triggers notification or vibration when the decoded `de` / `dx` callsign resolves to one of your selected DXCC entities
- `DXCC match target`
  - chooses whether DXCC matching checks the `transmitter`, the `receiver`, or `both`
- `Select DXCC`
  - opens the DXCC selection list
- `When a QSO is logged`
  - triggers notification or vibration after WSJT-X reports a completed logged QSO

### Ignore And Automation

- `Ignored callsigns`
  - opens the ignored callsign list
  - ignored entries are matched by `callsign + band`
- `Ignored callsign match target`
  - chooses whether ignored matching applies to the `transmitter`, the `receiver`, `both`, or neither
- `Auto-ignore the callsign after a logged QSO`
  - automatically adds the worked DX callsign on that band to the ignored list after a logged QSO

### Permissions And Maintenance

- `Open notification settings`
  - appears when Android notifications are disabled for the app
- `Open Log File`
  - opens the app log for troubleshooting
- `Reset Database`
  - clears cached grid information and the current decoded message list
- `Reset All`
  - resets settings, clears cached data, and stops the listener service
- `Add to whitelist` / `Add background`
  - opens Android battery or background settings that help the listener stay alive

## Matching Logic

- Ignored callsign matching target is configurable in settings: `transmitter only`, `receiver only`, `receiver/transmitter`, or `do not ignore`; ignored matching always uses `callsign + band`.
- Selected DXCC matching is configurable in settings: `transmitter only`, `receiver only`, or `receiver/transmitter`.
- Watched callsign regex matching is configurable in settings: `transmitter only`, `receiver only`, or `receiver/transmitter`.
- Watched callsign regex matching is applied to parsed `de` / `dx` callsigns, not to the full decoded message text.
- `Contains my callsign` detection is based on the full decoded message text.

## Relay Notes

- `Third-party data source` is implemented through the `wsjtx-relay` stack.
- The first successful relay connection stores the server fingerprint automatically.
- If the relay server certificate changes, use `Re-pair server` before reconnecting.
- After relay reconnect, the app receives current source state and snapshot data, but not historical decode replay.

## Todos

- ~~Add support for more message types~~
- ~~Third-party data source / relay support~~
- ~~Sync QSO records from Cloudlog / Wavelog~~
- Other enhancements

## Acknowledgments

- Thanks to the [ft8cn](https://github.com/N0BOY/FT8CN) project, from which some interface configurations and utility classes were borrowed
- WsjtxUtils (https://github.com/KC3PIB/WsjtxUtils) for WSJT-X UDP message handling libraries
- Codex: extensive refactoring was performed on the legacy codebase using Codex

## License

This project is licensed under `The Unlicense`.

``````
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <https://unlicense.org>
``````
