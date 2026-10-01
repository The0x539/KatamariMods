# KatamariDama60

Copyright © 2026 The0x539

As the sole author of this project as of the time of writing, I release my work under the terms of the GNU General Public License, version 3.
You should have received a copy of the GNU General Public License along with this software, in a "licenses" folder.
If not, see https://www.gnu.org/licenses/ .

At the time of writing, this project's source code is available at https://github.com/The0x539/KatamariMods .

This software takes the form of modifications to the game *Katamari Damacy REROLL* ("the base game").
The base game is property of BANDAI NAMCO Entertainment and is not subject to any of this project's licensing terms.

This project includes two first-party dynamically-linked libraries: *katamari-fps-patch* and *steamworks-wrapper*, referenced as Git submodules by the main codebase.
Their source code is managed separately, but both libraries **are** part of the work covered by the GNU GPL.
(This is despite the fact that I never bothered to add explicit licenses within their own repositories.)

## Third-party dependencies

Packaged releases of this software may include a binary named `steam_api64.dll`.
This is an unmodified copy of a proprietary shared library published by Valve Corporation as part of the Steamworks SDK.
This library is a runtime dependency, replacing an older version[^1] of the same shared library included as part of the base game.
It is included in the .zip file for end-user convenience and is not part of the work covered by the GNU GPL.

Parts[^2] of this project depend on [SimpleJson](https://github.com/facebook-csharp-sdk/simple-json), published under the MIT license.
This library's license should be distributed along with this software, in a "licenses" folder.

Parts[^3] of this project depend on [SDL](https://libsdl.org/) 3.x, published under the zlib license.
Packaged releases of this software may include an unmodified binary form of this library.
This library's license should be distributed along with this software, in a "licenses" folder.

All of these mods depend on [BepInEx](https://bepinex.dev/) as a mod loader.
BepInEx is distributed separately under the GNU Lesser General Public License, version 2.1.

## Additional permissions under GNU GPL version 3 section 7

<sub>*(The below permissions are granted retroactively for releases made before they were introduced.)*</sub>

If you modify this software, or any covered work, by bundling, packaging, combining, or linking it with any part of the Steamworks SDK,
the licensors of this software grant you additional permission to convey the resulting work.
 - *Practically:* Permission to **re**-redistribute the Steamworks binary I bundle in my releases is technically *not mine to give*,
   but assuming you have your own [permission from Valve](https://partner.steamgames.com/documentation/sdk_access_agreement) to do so,
   I authorize inclusion of (and linking against) Steamworks SDK redistributables when sharing these mods in any form.

This software is designed to dynamically link against the base game.
The licensors of this software grant you additional permission to link it with the base game.
 - *Practically:* Without this exception, you would still be allowed to *use* the mods per section 2,
   but *sharing* them[^4]  would be an automatic GPL violation[^5],
   because the [fundamental](https://www.gnu.org/licenses/gpl-faq.en.html#GPLPlugins) [dependency](https://www.gnu.org/licenses/gpl-faq.en.html#GPLPluginsInNF)
   on the base game would contradict subsection 5c.

[^1]: Upgrading Steamworks is necessary specifically for the `GamepadSupport` mod, as it relies on some features of the Steam Input API that did not exist yet in the Steamworks version that the base game ships with.
[^2]: `SingleplayerCousins` uses SimpleJson as the foundation for its custom-made glTF loader.
[^3]: `GamepadSupport` uses SDL to communicate with controllers, replacing the base game's usage of Rewired.
[^4]: Except as verbatim source code.
[^5]: If done by anyone other than the mods' original author.

