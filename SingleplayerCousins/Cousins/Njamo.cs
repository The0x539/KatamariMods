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
    [HarmonyPatch(typeof(EndingController), nameof(EndingController.Start))]
    public static void DressInCredits(EndingController __instance) {
        Dress(__instance._tran_itokoParent.Find("OUJI19").gameObject);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Start))]
    public static void SpawnLight(HUDManager __instance) {
        var gw = GlobalWork.Instance;
        if (gw.u8GameInfoMode != GAMEINFO_MODE.GAMEINFO_MODE_1P) return;
        if (gw.u8GameMode == GI_GMODE.GI_GMODE_ENDING) return;
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
        Renderer[] bodyParts = ouji.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (bodyParts.Length == 0) {
            bodyParts = ouji.GetComponentsInChildren<MeshRenderer>();
        }

        bodyParts = bodyParts
            .Where(rend => rend.sharedMaterial.name.StartsWith("OUJI19_body"))
            .ToArray();

        var mat = bodyParts[0].sharedMaterial;
        mat.shader = Shader.Find("Standard");
        mat.SetFloat("_Glossiness", 0);

        foreach (var part in bodyParts) {
            if (part is SkinnedMeshRenderer smr) {
                smr.sharedMesh.RecalculateNormals();
            } else if (part is MeshRenderer mr) {
                mr.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
            }
        }
    }
}
