using GamepadSupport.SteamInput;

using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamepadSupport;

public static class Glyphs {
    private static readonly Dictionary<string, Texture2D> glyphCache = [];

    public static Texture2D Get(ActionOrigin origin, GlyphSize size) {
        // TODO: Figure out what the flags argument does. Hopefully it could be of some use for choosing the "theme".
        string path = ISteamInput.Instance.GetGlyphPNGForActionOrigin(origin, size, 0);
        {
            var colorPath = path
                .Replace("knockout\\ps_button", "light\\ps_color_button")
                .Replace("knockout\\shared_color_button", "light\\shared_color_button");
            if (File.Exists(colorPath)) path = colorPath;
        }
        if (SceneManager.GetActiveScene().name == "Title2") {
            path = path.Replace("knockout", "dark");
        }

        if (glyphCache.TryGetValue(path, out var existing)) return existing;

        var tex = new Texture2D(0, 0);
        var data = File.ReadAllBytes(path);
        ImageConversion.LoadImage(tex, data);

        glyphCache[path] = tex;
        return tex;
    }
}