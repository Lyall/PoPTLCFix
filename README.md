# Prince of Persia: The Lost Crown Fix
[![Patreon-Button](https://github.com/user-attachments/assets/629633d4-b8de-46bf-9251-26c9d7b7b573)](https://www.patreon.com/Wintermance) 
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/W7W01UAI9)<br />
[![Github All Releases](https://img.shields.io/github/downloads/Lyall/PoPTLCFix/total.svg)](https://github.com/Lyall/PoPTLCFix/releases)

This is a BepInEx plugin for Prince of Persia: The Lost Crown that adds custom resolutions, ultrawide/narrower support and more.<br />

## Features
- Custom resolution support.
- Ultrawide and narrower aspect ratio support.
- Intro skip.

## Installation
- Grab the latest release of HundredHeroesFix from [here.](https://github.com/Lyall/PoPTLCFix/releases)
- Extract the contents of the release zip in to the the game folder. (e.g. "**steamapps\common\Prince of Persia The Lost Crown**" for Steam).
- 🚩First boot of the game may take a few minutes as BepInEx generates an assembly cache!

### Steam Deck/Linux Additional Instructions
🚩**You do not need to do this if you are using Windows!**
- Open up the game properties in Steam and add `WINEDLLOVERRIDES="winhttp=n,b" %command%` to the launch options.

## Configuration
- See **`GameFolder`\BepInEx\config\PoPTLCFix.cfg** to adjust settings for the fix.

## Known Issues
Please report any issues you see.
This list will contain bugs which may or may not be fixed.

- Video cinematics are stretched.
- Map markers appear outside the 16:9 boundary.

## Screenshots

| ![ezgif-2-b3ef5a727d](https://github.com/user-attachments/assets/9a0e658d-0a7c-46df-8666-96e04f87c591) |
|:--:|
| Gameplay |

## Credits
[BepinEx](https://github.com/BepInEx/BepInEx) for plugin loading.
