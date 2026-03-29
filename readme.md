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

+ Android 8.0 and above

## How to Use

**Note: Before using the software, if your phone has battery saver mode enabled, please make sure to disable it. Otherwise, it may cause frequent disconnections!**

1. Click the menu in the upper-right corner and select the settings page. Enter your callsign and Maidenhead locator
   coordinates on the settings page, and adjust other settings as needed. Note the IP address and port number displayed
   on this page.

2. Return to the main interface and click the menu again, then select "Start Service."

3. In your computer's JTDX/WSJTX software, enter the corresponding IP address and port number as shown below:

   <img src="./md_assets/page4.png" style="zoom: 67%;" />

   ~~Please check "accept udp requests" checkbox as well! (NOT NEEDED NOW)~~

   **Ensure that the computer running wsjtx and your phone are on the same local network, such as being connected to the
   same Wi-Fi!**

4. Wait for the information to appear on the software interface!

## Todos

+ ~~Add support for more message types(done)~~
+ Third-party data source / CLH Plugin support
+ Sync QSO Records from cloudlog/wavelog
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

