using System.Linq;

using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace SingleplayerCousins.Cousins;

public static class Vanta {
    // Some scenes have lighting set up such that we will need to use the Jungle billboard setup.
    // However, for other scenes, including main gameplay, it's easier and more reliable to just use the standard material in black mode.
    private static readonly string[] billboardScenes = ["Title3", "UI_MainMenu", "UI_OujiStar", "Result2"];

    public static void Dress(GameObject ouji) {
        Material? bodyMat = null;
        if (billboardScenes.Contains(ouji.scene.name)) {
            Jungle.Dress(ouji, ["antena_m", "body_m", "hand_m", "head_m", "leg_m", "nose_m"]);
            var billboard = ouji.transform.Find("JungleBoardEnding/JungleBoardPanel").GetComponent<MeshRenderer>();

            // Texture.blackTexture has 0 in the alpha channel, which doesn't work in menus.
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0, 0, 0, 1));
            tex.Apply();
            billboard.material.mainTexture = tex;

            // These hideFlags assignments are both load-bearing for the billboard
            // It seems to even be important that it applies to the GameObject and not a component. Yikes.
            billboard.gameObject.hideFlags = HideFlags.HideAndDontSave;
            billboard.transform.parent.gameObject.hideFlags = HideFlags.HideAndDontSave;
        } else {
            bodyMat = UnityObject.Instantiate(Material.GetDefaultMaterial());
            bodyMat.color = Color.black;
            foreach (var smr in ouji.transform.Find("body_root").GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)) {
                smr.sharedMaterial = bodyMat;
            }
        }

        var faceMat = UnityObject.Instantiate(bodyMat ?? Material.GetDefaultMaterial());
        faceMat.color = Color.white;
        faceMat.EnableKeyword("_EMISSION");
        faceMat.SetColor("_EmissionColor", Color.white);
        foreach (var smr in ouji.transform.Find("face_root/face").GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)) {
            smr.sharedMaterial = faceMat;
        }

        ouji.transform.Find("JNT_root/JNT_waist/JNT_spine_01/JNT_spine_02/JNT_neck/JNT_head/JNT_antenna/Ef_Highlight").gameObject.SetActive(false);
    }


    public static void DressBall(GameObject ball) {
        var mat = UnityObject.Instantiate(Material.GetDefaultMaterial());
        mat.color = Color.black;
        ball.GetComponent<MeshRenderer>().material = mat;
    }
}
