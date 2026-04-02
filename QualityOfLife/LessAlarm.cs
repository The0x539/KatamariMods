using BepInEx.Configuration;

using HarmonyLib;

using System.Reflection.Emit;

namespace QualityOfLife;

public static class LessAlarm {
    private static readonly ConfigEntry<int> startPlayingAtNSeconds = Plugin.configFile.Bind(
        "Alarm",
        "StartPlayingAtNSeconds",
        defaultValue: 30,
        new ConfigDescription(
            "Only start playing the alarm sound when this many seconds remain",
            new AcceptableValueRange<int>(0, 30)
        )
    );

    private static readonly ConfigEntry<int> playEveryNSeconds = Plugin.configFile.Bind(
        "Alarm",
        "PlayEveryNSeconds",
        defaultValue: 1,
        new ConfigDescription(
            "Play the sound every N seconds instead of every second",
            new AcceptableValueRange<int>(1, 10)
        )
    );

    public static readonly ConfigEntry<float> Volume = Plugin.configFile.Bind(
        "Alarm",
        "Volume",
        defaultValue: 1f,
        new ConfigDescription(
            "Change the volume of the alarm sound",
            new AcceptableValueRange<float>(0, 1)
        )
    );

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sReqTimeSe))]
    public static bool PlayLessOften(GameManager __instance) {
        var time = __instance.gWork.s32RemainTime;
        if (time > 30 * startPlayingAtNSeconds.Value) return false;
        if ((time % (30 * playEveryNSeconds.Value)) != 0) return false;
        return true;
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.sReqTimeSe))]
    public static IL PlayMoreQuietly(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false, [new(OpCodes.Ldc_R4, 1f)]) // First load of 1f is the "pitch" arg
            .Advance(1) // Without advancing, MatchForward will just find the same instruction twice
            .MatchForward(false, [new(OpCodes.Ldc_R4, 1f)]) // Second load of 1f is the "volume" arg
            .RemoveInstruction()
            .InsertAndAdvance(new(OpCodes.Ldsfld, Member.Field(() => Volume)),
                              new(OpCodes.Call, Member.Getter((ConfigEntry<float> e) => e.Value)))
            .Instructions();
    }
}
