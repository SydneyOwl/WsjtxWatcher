# WsjtxWatcher

## Introduction

WsjtxWatcher is an simple Android companion app for `WSJT-X` and `JTDX`.

It listens to WSJT-X UDP messages on your local network and shows decoded traffic on your phone in real time.
Users can view real-time FT8 information within the software and can also specify scenarios that require notifications (e.g. when a message matches your watched callsign patterns or selected DXCC entities), making it especially suitable for VHF DX.

When the software is running in the foreground, you can view the received FT8 information in real time. When the software goes to the background or the phone is locked, 
the software will push notifications to you when receiving specified FT8 information based on your settings.

Currently, the software supports Simplified Chinese and English.

<img src="./md_assets/page3.png" style="zoom: 60%;" />

## Supported System Versions

+ Android 8.0 and above.

## How to Use

**Note: Before using the software, if your phone has battery saver mode enabled, please make sure to disable it. Otherwise, it may cause frequent disconnections!**

1. Click the menu in the upper-right corner and select the settings page. Enter your callsign and Maidenhead locator
   coordinates on the settings page, and adjust other settings as needed. Note the IP address and port number displayed
   on this page.

2. Return to the main interface and click the menu again, then select "Start Service."

3. In your computer's JTDX/WSJTX software, enter the corresponding IP address and port number as shown below:

   <img src="./md_assets/page4.png" style="zoom: 67%;" />

   **Ensure that the computer running wsjtx and your phone are on the same local network, such as being connected to the
   same Wi-Fi!**

4. Wait for the information to appear on the software interface!

Tip: On the main message list, long-press a decoded record to quickly add that callsign to the ignored list (for the current band).

## Settings Guide

- `LAN IP` / `Server Port`: the UDP address that WSJT-X or JTDX should send decoded messages to.
- `Callsign`: your own station callsign. It is used for highlighting messages that contain your callsign and as the default watched callsign pattern when no custom pattern list exists yet.
- `Location`: your own 4-character Maidenhead grid. It is used to calculate distance to the decoded station when possible.
- `Language`: switches the app language between Simplified Chinese and English. When the language is changed, the app saves settings, stops the background service, and exits so you can reopen it with the new language applied.

### Notification Triggers

- `When the message matches specified callsigns`: triggers notification/vibration when the decoded `de` / `dx` callsign matches one of your configured watched callsign regex patterns.
- `Specified callsign match target`: controls whether watched callsign matching checks the `transmitter`, the `receiver`, or `both`.
- `Regex`: opens the watched callsign regex list editor.
- `When a WSJT-X message is received`: triggers notification/vibration for every decoded message. This is mainly useful for VHF DX style monitoring.
- `When selected DXCC appears`: triggers notification/vibration when the decoded `de` / `dx` callsign resolves to one of your selected DXCC entities.
- `DXCC match target`: controls whether selected DXCC matching checks the `transmitter`, the `receiver`, or `both`.
- `Select DXCC`: opens the DXCC selection list used by the selected DXCC trigger.
- `When a QSO is logged`: triggers notification/vibration after WSJT-X reports a completed logged QSO.

### Ignore And Automation

- `Ignored callsigns`: opens the ignored callsign list. Ignored items are matched by `callsign + band`.
- `Ignored callsign match target`: controls whether ignored callsigns suppress alerts for the `transmitter`, the `receiver`, `both`, or neither.
- `Auto-ignore the callsign after a logged QSO`: after a QSO is logged, automatically add the worked DX callsign on that band to the ignored list.

### Permissions And Maintenance

- `Open notification settings`: appears when system notifications are disabled for the app.
- `Open Log File`: opens the application log for troubleshooting.
- `Reset Database`: clears cached grid information and the current decoded message list.
- `Reset All`: resets settings, clears cached data, and stops the listener service.
- `Add to whitelist` / `Add background`: open Android battery/background settings that help the listener stay alive.

## Matching Logic

- Ignored callsign matching target is configurable in settings: **transmitter only**, **receiver only**, **receiver/transmitter**, or **do not ignore**; matching always uses callsign + band.
- Selected DXCC matching is configurable in settings: **transmitter only**, **receiver only**, or **receiver/transmitter**.
- Watched callsign regex matching is configurable in settings: **transmitter only**, **receiver only**, or **receiver/transmitter**.
- Watched callsign regex matching is applied to the parsed `de` / `dx` callsigns, not to the full decoded message text.
- "Contains my callsign" detection is also based on full decoded message text.

## Todos

+ ~~Add support for more message types(done)~~
+ Third-party data source / CLH Plugin support
+ ~~Sync QSO Records from cloudlog/wavelog~~
+ Other enhancements...

## Acknowledgments

+ Thanks to the [ft8cn](https://github.com/N0BOY/FT8CN) project, from which some interface configurations and utility
  classes were borrowed.
+ WsjtxUtils (https://github.com/KC3PIB/WsjtxUtils) for WSJT-X UDP message handling libraries
+ codex - Extensive refactoring was performed on the legacy codebase using Codex.


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
