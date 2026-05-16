# WsjtxWatcher

`WsjtxWatcher` is an Android companion app for `WSJT-X` and `JTDX`.

It lets you watch live decode traffic on your phone and receive alerts when a message or a logged QSO matches the rules you configured. 
It is designed for daily station monitoring, remote watching, DX hunting, and VHF-style activity watching.

Currently supported languages:

- Simplified Chinese
- English

Suggested screenshot:

- Main screen with live decode list

## What The App Can Do

- Receive live WSJT-X / JTDX traffic over local UDP
- Receive live traffic from a remote relay source
- Show decoded messages in real time
- Highlight and notify on messages that match alert rules
- Alert on selected DXCC entities
- Alert on logged QSOs
- Maintain an ignored-callsign list
- Auto-ignore worked stations after a logged QSO
- Keep running in the background when Android permissions allow it

## Android Support

- Android 8.0 and above

## Data Source Modes

The app supports two ways to receive data.

### 1. UDP

Use this mode when your phone and the computer running `WSJT-X` / `JTDX` are on the same local network.

In this mode the app listens on the phone's local IP and UDP port and `WSJT-X` or `JTDX` sends UDP packets directly to the phone

Suggested screenshot:

- Settings page showing UDP mode

### 2. Third-party Data Source

See: [wsjtx-relay](https://github.com/SydneyOwl/wsjtx-relay)

Use this mode when the source station is remote, or when direct LAN UDP is not possible.

In this mode:

- a remote `wsjtx-relay-server` accepts watcher connections
- a `wsjtx-relay-client` runs near `WSJT-X` / `JTDX`
- `wsjtx-relay-client` uploads live events to the relay server
- `WsjtxWatcher` connects as a watcher and follows one selected source

Suggested screenshot:

- Settings page showing relay mode
- Source selection page

## Quick Start

### Option A: Local UDP

1. Open `Settings`.
2. Set `Data source` to `UDP`.
3. Return to the main page and tap `Start Service`.
4. In `WSJT-X` or `JTDX`, set the UDP destination to the phone's displayed LAN IP and server port.

Make sure your computer and your phone are on the same LAN.

Suggested screenshot:

- Main page showing service controls
- WSJT-X UDP settings example

### Option B: Relay / Third-party Data Source

**please see https://github.com/SydneyOwl/wsjtx-relay for more**

1. Deploy and start `wsjtx-relay-server` on your own server(public ip needed).
2. Run `wsjtx-relay-client` on the computer that runs jtdx/wsjtx.
3. Open `Settings` in `WsjtxWatcher` and set `Data source` to `Third-party data source`.
4. Fill configs and tap `Test connection`. if succeed you can click `Select source` to select the client your configured before.
5. Return to the main page and tap `Start Service`.

On the first successful relay connection, the app stores the server fingerprint automatically. 
if the server certificate changes later(e.g. redeployed wsjtx-relay-server), use `Re-pair server` and test again

Suggested screenshot:

- Relay settings form
- Test connection success state
- Relay source selection dialog

## Main Screen

The main screen is used for live monitoring.

Suggested screenshot:

- Main monitoring screen

## Settings Guide

### Common Settings

### UDP Settings

- `LAN IP` / `Server Port` is the **local** UDP endpoint that `WSJT-X` / `JTDX` should send to

### Third-party Data Source Settings

- `Server URL`: relay server base URL. e.g. `wss://example.com:8443`
- `Shared Secret`: relay authentication secret
- `Tenant ID`: private shared identifier used by both the uploader side and the watcher side
- `Relay status`: current relay connection state
- `Test connection`:validates URL, certificate trust, and shared-secret authentication
- `Select source`: opens the relay source picker
- `Refresh source list`:reloads the relay source catalog
- `Re-pair server`: clears the stored trusted fingerprint so the app can pair again

Suggested screenshot:

- Relay settings block with buttons

## Alert Basics

- `Settings -> Configure alert rules`

From there you can view/edit built-in rules or create your own rules

**please note that When multiple rules match at the same time only the highest-priority rule triggers(lower `Priority` number means higher priority)**

Suggested screenshot:

- Rule list page
- Rule card with `Edit rule` button and enable switch

### Configure alert rules

#### Rule Trigger Types

There are two trigger types:

- `Decode message rule`is checked every time a new decoded message arrives
- `Logged QSO rule` is checked every time `WSJT-X` reports a logged QSO

#### Rule Actions

Each rule can do one or both of the following:

- `Send notification`
- `Vibration`

#### Cooldown

`Cooldown` prevents the same rule from repeatedly notifying within a short time window.

for example if a rule has `Cooldown = 10`  and it already triggered once then the same rule will not trigger again for the next 10 seconds

#### Condition Tree

Each rule contains a condition tree.

It is made of groups and predicates. one group can have multiple predicates.

##### Predicates

A predicate is `field + operator + value`

For example we have following predicate:

- field: `Transmitter callsign`
- operator: `Regex`
- value: `^BA1`

this Means transmitter callsign begins with `BA1` will be matched (e.g. CQ BA1xxx)

Suggested screenshot:

- Rule editor page
- Condition tree with one group and several predicates

##### Groups

A group controls how its child conditions are combined.

- `Match all`
  - all child conditions must be true
  - equivalent to logical `AND`
- `Match any`
  - any child condition may be true
  - equivalent to logical `OR`

Groups can be nested, so more advanced matching logic is possible.

Example:

```
`(Transmitter callsign starts with JA OR Receiver callsign starts with JA) AND Mode = FT8`
  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^      ^^^^^^^^^^^
              predicate 1                           predicate 2                predicate 3
  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^      ^^^^^^^^^^^
                                    group 1                                       group 2
```

##### Named Sets

Named sets are reusable global lists that a rule can reference.

This is useful when you want to maintain one shared list and let multiple rules use it.

Currently supported named sets:

- `Ignored callsigns`
- `DXCC list`

###### Ignored Callsigns

Open:`Settings -> Ignored callsigns`

From there you can modify entries manually or import ignored callsigns from `Cloudlog` / `Wavelog`

There is also `Auto-ignore the callsign after a logged QSO`. When enabled, the app automatically adds the worked DX callsign on that band to the ignored list after a logged QSO.

later you can use `Ignored Callsigns` as predicate values.

Suggested screenshot:

- Ignored callsign import page

###### DXCC List


##### Built-in Rule Ideas

The exact defaults may change over time, but the built-in rule set is designed around these common scenarios:

- my callsign appears
- watched callsign logic
- any message
- selected DXCC appears
- logged QSO

These rules are intended as ready-made starting points. You can keep them as-is, edit them, or disable the ones you do not want.

## Matching Notes

- Watched-callsign matching works on parsed `de` / `dx` callsigns, not on the full decoded message text.
- `Contains my callsign` style logic uses the full decoded message text.
- Ignored-callsign matching can be configured by target and can optionally include band.
- DXCC matching can be configured against transmitter, receiver, or both, depending on the rule structure you build.
- If multiple rules match the same event, only the highest-priority rule triggers.

## Relay Notes

- `Third-party data source` is implemented through the `wsjtx-relay` stack.
- The first successful relay connection stores the server fingerprint automatically.
- If the relay server certificate changes, use `Re-pair server` before reconnecting.
- After relay reconnect, the app receives current source state and snapshot data, but not historical replay.


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
