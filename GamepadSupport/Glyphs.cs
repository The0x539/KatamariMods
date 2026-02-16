using GamepadSupport.SteamInput;

using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace GamepadSupport;

public static class Glyphs {
    private static readonly Dictionary<GlyphSize, Dictionary<ActionOrigin, Texture2D>> glyphCache = new() {
        [GlyphSize.Small] = [],
        [GlyphSize.Medium] = [],
        [GlyphSize.Large] = [],
    };

    public static Texture2D Get(ActionOrigin origin, GlyphSize size) {
        var cache = glyphCache[size];
        if (cache.TryGetValue(origin, out var existing)) return existing;

        // TODO: Figure out what the flags argument does...
        string path = ISteamInput.Instance.GetGlyphPNGForActionOrigin(origin, size, 0);
        {
            var colorPath = path.Replace("ps_button", "ps_color_button");
            if (File.Exists(colorPath)) path = colorPath;
        }
        var tex = new Texture2D(0, 0);
        var data = File.ReadAllBytes(path);
        ImageConversion.LoadImage(tex, data);

        cache[origin] = tex;
        return tex;
    }
}