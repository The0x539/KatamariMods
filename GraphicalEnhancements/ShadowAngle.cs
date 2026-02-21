using HarmonyLib;

using UnityEngine;

namespace GraphicalEnhancements;

// Rotate the shadow that the katamari casts to follow whatever slope you're currently on.
// Without this patch, the shadow will frequently clip into the ground, which is visibly ugly.
static class ShadowAngle {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static void UpdateShadow(Player __instance) {
        var shadow = __instance.ShadowTransform;
        var ray = new Ray(shadow.position, Vector3.down);

        Quaternion target;
        if (Physics.Raycast(ray, out var hit, 100f)) {
            target = Quaternion.FromToRotation(Vector3.up, hit.normal);
        } else {
            target = Quaternion.identity;
        }

        // I'm not too worried about this being *properly* delta-timed; it just needs to be vaguely "smooth"./
        // (I did try the exponential trick, but it didn't seem to work. It's hard to tell with this shadow, though.)
        shadow.rotation = Quaternion.Slerp(shadow.rotation, target, 2 * Time.deltaTime);
    }
}
