# PlateWatch

Your Bambu Lab printers, one click away in the Windows system tray.

A native Windows 11 app (WinUI 3, .NET 10): live progress and time left in the
tray, a compact popup for quick checks, and a full window for everything else.
Fleet-first: every printer glanceable at once. Talks to printers over your
local network only - no account, no cloud, no subscription.

Status: early build. The design is locked (fluent, colorblind-safe by rule:
every state is color + icon + word, never color alone) and the core library is
implemented and tested. The WinUI 3 shell is next.

## Layout

- `src/PlateWatch.Core` - UI-free core: fleet + printer models, client
  interface (mock now, LAN MQTT later), notification policy, spool usage
  ledger, design tokens. Builds and tests on any OS.
- `tests/PlateWatch.Tests` - xUnit suite for the core.
- `src/PlateWatch.App` - WinUI 3 tray app (upcoming).

## Build and test

```
dotnet test
```

License: MIT. Independent, community-developed software; not affiliated with
or endorsed by Bambu Lab.
