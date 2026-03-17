using HarmonyLib;

using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.PostProcessing;

namespace GraphicalEnhancements;

public static class TVFixes {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnPostRender))]
    public static void FixTVResolution(GameManager __instance) {
        if (__instance.texCapture is not RenderTexture tv) return;

        var res = QualitySetting.Instance.Resolution;
        if (res != new Vector2(tv.width, tv.height)) {
            tv.Release();
            tv.width = (int)res.x;
            tv.height = (int)res.y;
            tv.Create();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
    public static void FixTVColorSpace(GameManager __instance) {
        if (__instance.texCapture is not RenderTexture tv) return;
        tv.descriptor = tv.descriptor with { sRGB = true };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PostProcessingBehaviour), nameof(PostProcessingBehaviour.OnRenderImage))]
    public static void ShowPostProcessingOnTV(PostProcessingBehaviour __instance, RenderTexture destination) {
        if (__instance.GetComponent<GameManager>() is not GameManager game) return;
        if (game.texCapture is not RenderTexture tv) return;

        Graphics.Blit(destination, tv);
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.OnPostRender))]
    public static IL SkipRedundantBlit(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldnull),
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, Member.Field<GameManager>(gm => gm.texCapture)),
                          new(OpCodes.Call, Member.Method((RenderTexture dest) => Graphics.Blit(null, dest))))
            .RemoveInstructions(4)
            .Instructions();
    }
}

