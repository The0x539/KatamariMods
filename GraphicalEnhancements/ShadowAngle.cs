using HarmonyLib;

using UnityEngine;

namespace GraphicalEnhancements;

// Rotate the shadow that the katamari casts to follow whatever slope you're currently on.
// Without this patch, the shadow will frequently clip into the ground, which is visibly ugly.
static class ShadowAngle {
    private static readonly int mask = ~LayerMask.GetMask("PlayerProp", "Ignore Raycast");

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static void UpdateShadow(Player __instance) {
        var shadow = __instance.ShadowTransform;

        // The shadow is right on the ground, so we need to start *slightly* above the ground.
        // Otherwise, most of the time, the raycast will just fail, and the angle will default to horizontal.
        // However, if a sloped surface extends past the ground to under it, the raycast would hit that instead.
        // A good example of this is directly behind the starting location in Make A Star 7.
        var ray = new Ray(shadow.position + Vector3.up, Vector3.down);

        var target = Quaternion.identity;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance: float.PositiveInfinity, layerMask: mask)) {
            target = Quaternion.FromToRotation(Vector3.up, hit.normal);
        }

        // I'm not too worried about this being *properly* delta-timed; it just needs to be vaguely "smooth"./
        // (I did try the exponential trick, but it didn't seem to work. It's hard to tell with this shadow, though.)
        shadow.rotation = Quaternion.Slerp(shadow.rotation, target, 2 * Time.deltaTime);
    }
}
