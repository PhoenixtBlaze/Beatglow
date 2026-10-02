# BeatGlow

BeatGlow shows either a logo or a gamertag on both sides of the Beat Saber runway. White pixels in the logo, and the typed name, move through a rainbow equalizer driven by the song. Colored artwork stays as you drew it. The mod does not change environment lights, fog, or the chart's light show.

Built for Beat Saber 1.40.8.

## Requirements

- [BSIPA](https://github.com/nike4613/BeatSaber-IPA-Reloads) 4.3.6 or newer
- [BeatSaberMarkupLanguage](https://github.com/monkeymanboy/BeatSaberMarkupLanguage) 1.12.5 or newer
- [BS Utils](https://github.com/Kylemc1413/Beat-Saber-Utils) 1.14.2 or newer
- [SiraUtil](https://github.com/Auros/SiraUtil) 3.1.0 or newer

## Install

1. Download `BeatGlow.zip` from the [latest release](https://github.com/PhoenixtBlaze/Beatglow/releases/latest).
2. Copy `BeatGlow.dll` from `BeatGlow/Plugins/` inside the zip into your game's `Plugins` folder.
3. Start the game once so BSIPA loads the plugin.

The DLL must sit directly in `Plugins`. Extracting the zip on top of the game folder will nest it one level too deep.

## Logos

Put PNG files in:

`UserData/BeatGlow`

Use a transparent background. Pixels that are white, or very close to white, are the parts that change color with the music. Any other color is left alone. The menu lists the first six PNGs in that folder.

## In game

Open **Mods**, then **BeatGlow**.

- **Logo** shows the PNG you select. The last file you click is saved and used again after a restart.
- **Gamertag** shows the name you type, up to 32 characters. Logo and gamertag are never shown together.
- **Stand up** turns the logo or name upright, facing you. Leave it off to keep them flat on the runway.

The choice applies on the next song. Both copies sit beside the notes, not on top of them.

## Building

The project targets .NET Framework 4.8 and references the game's managed assemblies. Set `BeatSaberDir` to your Beat Saber install if it is not the default path in `BeatGlow/BeatGlow.csproj`, then build the `BeatGlow` project in Release. The build copies `BeatGlow.dll` into that install's `Plugins` folder.
