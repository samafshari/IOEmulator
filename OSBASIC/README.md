# OSBASIC (OpenSilver)

This folder contains the OpenSilver UI shell and two hosts:

- `OSBASIC` (netstandard2.0): OpenSilver/Silverlight XAML UI that displays the emulator framebuffer and forwards input.
- `OSBASIC.Simulator` (net9.0-windows): Desktop simulator to run/test the UI.
- `OSBASIC.Browser` (net9.0 browser-wasm): Blazor WebAssembly host that runs the QBASIC emulator and interpreter.

The emulator (`IOEmulator.Lib`) and QBASIC interpreter (`IOEmulator.QBasic`) are referenced by the Browser host (not the UI) to keep OpenSilver UI compatible with netstandard2.0.

## Run (Browser)

```powershell
# From repo root
dotnet build .\OSBASIC\OSBASIC.Browser\OSBASIC.Browser.csproj -c Debug
 dotnet run --project .\OSBASIC\OSBASIC.Browser\OSBASIC.Browser.csproj -c Debug
```

Then open the URL shown in the console. The default program "GUESS" will load.

## Run (Simulator)

```powershell
# From repo root
 dotnet run --project .\OSBASIC\OSBASIC.Simulator\OSBASIC.Simulator.csproj -c Debug
```

Note: The Simulator currently hosts only the OpenSilver UI page; the emulator is driven by the Browser host in this setup.

## Controls

- Typing letters A–Z and digits works (Shift supported for uppercase and !@#$%^&*()).
- Special keys: Enter, Backspace, Delete, Home, End, Tab, Escape, Arrow keys.
- Mouse: position and left/right button state are forwarded to the emulator.

## Notes

- Sound: implemented via WebAudio using JS interop. The Browser host calls functions in `wwwroot/js/webaudio.js` through `IJSRuntime` to play beeps/tones/music strings.
- Samples: the Browser host uses embedded BASIC samples from `IOEmulator.QBasic`.
- If you add menus (Samples/Speed/Refresh) in `MainPage.xaml`, raise events and handle them in the Browser host to keep the UI project netstandard-only.
