using UnityEngine;

namespace SingleplayerCousins.Cousins;

public static class Vanta {
    // Texture2D.blackTexture has 0 in the alpha channel, which doesn't work in menus.
    private static readonly Texture2D blackTexture = new(1, 1);
    static Vanta() {
        blackTexture.SetPixel(0, 0, new(0, 0, 0, 1));
        blackTexture.Apply();
    }

    public static void Dress(GameObject ouji) {
        var bodyParts = ouji.transform.Find("body_root").GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);

        var bodyMat = bodyParts[1].material;
        bodyMat.color = Color.black;
        bodyMat.mainTexture = blackTexture;
        foreach (var smr in bodyParts) {
            if (smr.name is "nose_m" or "antena_m") continue;
            smr.sharedMaterial = bodyMat;
        }

        var faceMat = UnityObject.Instantiate(bodyMat);
        faceMat.color = Color.white;
        faceMat.mainTexture = Texture2D.whiteTexture;
        faceMat.EnableKeyword("_EMISSION");
        faceMat.SetColor("_EmissionColor", Color.white);
        foreach (var smr in ouji.transform.Find("face_root/face").GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)) {
            smr.sharedMaterial = faceMat;
        }
    }

    public static void DressBall(GameObject ball) {
        var renderer = ball.GetComponent<MeshRenderer>();
        var mat = renderer.material;
        mat.color = Color.black;
        mat.mainTexture = blackTexture;
        renderer.material = mat;
    }
}
