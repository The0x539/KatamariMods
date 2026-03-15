using DefineEnum;

using HarmonyLib;

using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace SingleplayerCousins.Cousins;

public static class Njamo {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(KinokoRotator), nameof(KinokoRotator.CloneItoko))]
    public static void DressOnMushroom(KinokoRotator __instance) {
        foreach (var ouji in __instance._list_dataOuji) {
            if (ouji.kinokoMover.OujiIndex == (int)Cousin.Njamo) {
                Dress(ouji.objOuji);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static void DressInGameplay(Player __instance) {
        if (__instance.oujiNo == (int)Cousin.Njamo) {
            Dress(__instance.objOuji);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Start))]
    public static void SpawnLight(HUDManager __instance) {
        var gw = GlobalWork.Instance;
        if (gw.u8GameInfoMode != GAMEINFO_MODE.GAMEINFO_MODE_1P) return;
        if (gw.player[0].oujiNo != (int)Cousin.Njamo) return;

        var lightObj = new GameObject("Directional Light (Njamo)");
        var light = lightObj.AddComponent<Light>();

        lightObj.transform.rotation = Quaternion.Euler(60, 60, 0);
        light.type = LightType.Directional;
        light.color = new(1, 1, 0.9216f, 1);
        light.intensity = 1.3f;

        SceneManager.MoveGameObjectToScene(lightObj, SceneManager.GetSceneByName("UI_HUD"));
    }

    public static void Dress(GameObject ouji) {
        var bodyParts = ouji.GetComponentsInChildren<SkinnedMeshRenderer>()
            .Where(smr => smr.sharedMaterial.name.StartsWith("OUJI19_body"))
            .ToArray();

        var mat = bodyParts[0].sharedMaterial;
        mat.shader = Shader.Find("Standard");
        mat.SetFloat("_Glossiness", 0);

        foreach (var part in bodyParts) {
            part.sharedMesh.RecalculateNormals();
        }
    }
}
