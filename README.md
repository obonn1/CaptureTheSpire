# Capture the Spire

A Slay the Spire 2 mod for quickly sharing screenshots.

Capture the Spire copies captures to the Windows clipboard and also saves a PNG fallback. It automatically captures:

- the full map
- the full deck
- the current visible screen everywhere else

## Usage

Press **F8** or click the camera button beside the map button.

By default, both the hotkey and button are enabled. With [ModConfig](https://github.com/xhyrzldf/ModConfig-STS2) installed, you can change the capture key and enable or disable either control.

## Installation

Requires [BaseLib](https://github.com/Alchyr/BaseLib-StS2).

Place the release files together in:

```text
Slay the Spire 2/mods/CaptureTheSpire/
```

The folder should contain:

```text
CaptureTheSpire.dll
CaptureTheSpire.json
CaptureTheSpire.pck
```

## Building

Open the solution and build it. The project template copies the mod files into the configured game mods folder automatically.
