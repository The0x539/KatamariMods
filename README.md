This is a suite of mods to enhance *Katamari Damacy REROLL* on PC, addressing many of the remake's technical shortcomings.

# FramerateUncap
This mod patches the game's native-code world simulation to be framerate-independent, so the actual gameplay is no longer stuck at 30 FPS.
On its own, the game's framerate will be either uncapped or governed by VSync. In tandem with `GraphicalEnhancements`, the game will use either VSync or a customizable cap of up to 360 FPS.

Additionally, this mod adds *stereo rumble*: if you bump into something to your left or right, the controller vibration will be biased to that side.
This feature would be more at home in a different mod, but it lives here because it requires patches to the native world simulation to implement, and putting it here is easier than having two mods competing to patch `PS2KatamariSimulation.dll`.

# GraphicalEnhancements
This mod fixes several issues with the game's graphics and restores the Depth of Field effect that was present in the PS2 game but scrapped in the remake.

Features:
  - Add optional Depth of Field post processing. The PS2 game's version of this effect was more simplistic than Unity's implementation, but I've tried to get pretty close to "visual parity".
    - In action:
    
      <img height="500" alt="image" src="https://github.com/user-attachments/assets/edcf8930-a54f-48d8-a1f9-2c679b3ef735" />

    - On the PS2:

      <img width="597" height="448" alt="Katamari Damacy_SLUS-21008_20260324150624" src="https://github.com/user-attachments/assets/a2adc9b7-7e3c-44b4-b187-0a3d9820e758" />
  - Fix weird artifacts in the ambient occlusion effect, caused by banding in Unity's low-precision depth prepass.
    - Before/after (Looks significantly worse in motion):

      <img height="300" alt="image" src="https://github.com/user-attachments/assets/e2d08a93-3c18-4005-b7a8-20f0a8d5b06b" />
      <img height="300" alt="image" src="https://github.com/user-attachments/assets/97a118e6-9bc4-4d76-aad6-d6365c410e07" />
  - Apply anisotropic filtering to some of the game's textures where the effect (or its absence) is most visible.
    - It's applied *selectively* because Unity refuses to actually perform anisotropic filtering unless you also opt in to bi-/tri-linear texture sampling, which ruins all the pixel art textures for the game world. The best way I've found to counteract this is to nearest-neighbor-upscale the texture so that the bilinear blur isn't noticeable. This is a stupid waste of VRAM and doing it indiscriminately for *all* textures seems like a bad idea.
    - Before/after (Open at 100% zoom. Looks worse in motion.):

      <img height="300" alt="image" src="https://github.com/user-attachments/assets/901c7cc5-b928-4a89-aa1f-0d87acd9253e" />
      <img height="300" alt="image" src="https://github.com/user-attachments/assets/2daa3425-9490-4f1a-aebf-0cabfad7c30a" />
  - Rotate the shadow cast by the katamari to match the slope of the ground beneath it, so that it clips into the ground less often.
    - Hilariously, the PS2 game actually wins here: its shadow is a decal that conforms to the ground's contours, while I'm stuck just rotating a flat circle.
    - Before/after:
      
      <img height="200" alt="image" src="https://github.com/user-attachments/assets/9974bbcf-d74c-405e-875d-9b137fcfc8dc" />
      <img height="200" alt="image" src="https://github.com/user-attachments/assets/cda635b3-c245-4998-8363-e27d0838e440" />
  - Use higher-resolution + antialiased intermediate textures for many places where the HUD/menu shows a 3D object on a 2D canvas.
  - Add entries to the options menu to toggle depth-of-field and set the maximum framerate (if VSync is disabled).

# QualityOfLife
This mod is a pile of assorted enhancements not big enough to get their own mod, and was the sandbox for prototyping new mod features.

Features:
  - `IngameOptions`: Allow opening the options menu during a level by pressing the west face button in the pause menu.
    - `EarlyOptions`: In the tutorial, there is no pause menu, so instead, the options menu is accessed with SELECT, replacing the vibration toggle.
    - It's absolutely ridiculous that the vanilla game forced you to complete the tutorial *and the first level* before allowing you to access the *display settings*.
    - Ideally, it would be possible to open the menu immediately on first launch, but this is the best I could do in terms of cleanly integrating it into the existing game structure.
  - `FrustrationMusic`: When playing the Ursa Major, Taurus, or Polaris missions multiple times in a single session, **play a random music track** instead of the same one every time.
  - `KonamiCode`: Allow opening the game's "debug menu", left present in the game's code but inaccessible and half-broken.
  - `HideCursor`: Hide the mouse cursor if it's over the game window during gameplay. Moving the mouse will show it again.
  - `LocalizationTweaks`: Some minor changes to the English localization.
    - Correct "Stationary" to "Stationery"
    - Refer to "the North Star" as "Polaris" (selectively), because it fits better with the constellation names and sounds cooler.
    - Rename Colombo to Njamo, to match W❤️KR
  - Minor bug fixes:
    - On startup, the game tries to change its window title, but if you switch to another window before that happens, it will instead rename *that window* to `Katamari Damacy Reroll`.
    - If you press START while a "non-blocking message" is present (e.g. toggling vibration, or "BA" "NA" "NA" from Make A Star 7), the game will "buffer" the input and automatically skip the *next blocking message*.

# GamepadSupport
This mod replaces the vanilla game's usage of Rewired for gamepad input with SDL3.
Doing this allows me to expand the range of controllers that the game recognizes more easily, and users should be able to drop in future versions of SDL3.dll without any updates to the mod.

Without this mod, the game does not recognize the PS5's DualSense controller, instead relying on Steam Input to emulate an Xbox controller.
This happens for a very silly reason: It can recognize the DualShock 4, but only by checking if the controller's name contains the exact text `Sony DualShock`, which is false for the DualSense.

This mod also replaces the button prompts with images provided by the Steam Input API, which both looks nice and enhances compatibility.

At the time of writing, it's unfinished and disabled due to difficulties with Steam Input hiding the sensors from the game, but there's also WIP code to enable the Joy-Con motion controls from the Switch version.

# SingleplayerCousins
The Space Mushroom is populated by the Prince's cousins, but the game only allows you to play as them in versus mode.
This mod lifts that restriction: if you select a cousin on the Mushroom, you can fly over to Earth or the Home Planet and proceed to play as that cousin.

*Where possible*, Royal Presents will be stretched and repositioned to fit the alternate player models, but not every accessory works with every cousin.

Jungle and Njamo are also subject to some minor graphical enhancements.

## Pretenders
Additionally, this mod introduces *Pretenders* to the Throne of All Cosmos: you can supply a model for a custom player character by placing it in the `Pretenders` folder created in the game's install directory.

The models must use the FBX format, should have textures packed within the file, and the rigging is expected to conform to a particular structure. The structure is not yet documented, but for the most part, the skeleton should be set up like a player model ripped from the game, with matching bone names.
If the model is accompanied by a matching `.ball` file (e.g. `Soyo.fbx` and `Soyo.ball.fbx`), it will be used as that character's katamari skin.

Out of the box, the mod will add my OC, **Vanta**, to the Space Mushroom. Vanta serves as a sort of tech demo and proof of concept for custom visual effects on pretenders.

**NOTE:** Normally, the cousin-associated katamari skin is only used in Versus Mode and Eternal Mode.
For singleplayer missions, the game instead uses the skin associated *with that level*.
This mod overrides that behavior, only for *Make A Star 2* and *Make the Moon* by default.
If you want to **use your custom character's katamari skin in all levels**, edit `BepInEx/config/SingleplayerCousins.cfg`. (You can do this while the game is running.)

# Installation
1. Download [BepInEx **5**](https://github.com/BepInEx/BepInEx/releases). (As of March 2026, pre-release builds of BIE **6** __will not work__.)
2. Install BepInEx normally. (i.e., extract the ZIP into the game's install folder)
  a. *(Proton/Wine only)* Configure the game to load the mod loader. You may be able to find other ways to do this online, but my preferred way is specifying `WINEDLLOVERRIDES="winhttp=n,b" %command%` in Steam's "launch options" for the game.
  b. *(Optional)* Before installing any actual mods, confirm that the mod loader is working by launching the game and checking for a new `config/` folder inside `BepInEx/`.
3. Download the latest [release](https://github.com/The0x539/KatamariMods/releases) of my mods.
4. Extract the ZIP into the game's install folder, same as with BepInEx.
  a. You will be prompted to overwrite `CSteamworks.dll` and `steam_api64.dll`. Replacing these files is necessary for the `GamepadSupport` mod.
  b. This installs *all* the mods. If (for some reason) you want to install only some of them, don't extract the corresponding mod, and you don't have to extract its dependencies either:
    i. `GamepadSupport` depends on `SDL3.dll`, `CSteamworks.dll`, and `steam_api64.dll`.
    ii. `SingleplayerCousins` depends on `AssimpNet.dll` and `assimp.dll`.
    iii. `FramerateUncap` depends on `katamari_ffi.dll`.
