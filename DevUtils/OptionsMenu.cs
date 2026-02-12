using DefineEnum;

using HarmonyLib;

using MyGame;
using MyGame.InputStatus;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DevUtils;

static class OptionsMenu {
    public static void AddListener() {
        SceneManager.activeSceneChanged += (a, b) => {
            // The active scene determines the lighting settings.
            // Without this, opening the options menu mid-game sends you to "the dark world" where everything has moody lighting.
            if (a.name is "GameMain" && b.name is "Option" or "Quality") {
                SceneManager.SetActiveScene(a);
            }

            if (a.name is "GameMain" && b.name is "Quality") {
                foreach (var obj in b.GetRootGameObjects()) {
                    if (obj.name == "Main Camera") {
                        obj.SetActive(false);
                        break;
                    }
                }
            }
        };
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.PauseProc))]
    public static IEnumerable<CodeInstruction> AddGuy(IEnumerable<CodeInstruction> instructions) {
        var matcher = new CodeMatcher(instructions);

        matcher
            .MatchForward(false,
                          new(OpCodes.Ldc_I4_7),
                          new(OpCodes.Callvirt, AccessTools.Method(typeof(InputPadBase), nameof(InputPadBase.IsDown))))
            .MatchBack(false,
                       new(OpCodes.Ldarg_0),
                       new(OpCodes.Ldfld, AccessTools.Field(typeof(PauseMenu), nameof(PauseMenu.input))));

        var start = matcher.Pos;
        var end = matcher.MatchForward(false, [new(OpCodes.Stloc_S)]).Pos;
        var flagLocalVar = matcher.Operand;

        // This patch lets you exit the normal pause menu by pressing B, in addition to the usual START.
        matcher
            .RemoveInstructionsInRange(start, end)
            .Start()
            .Advance(start)
            .Insert(new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldloc_3),
                    new(OpCodes.Call, AccessTools.Method(typeof(OptionsMenu), nameof(PauseMenuPatchA))),
                    new(OpCodes.Stloc_S, flagLocalVar));

        matcher
            .MatchForward(false,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, AccessTools.Field(typeof(PauseMenu), nameof(PauseMenu.gWork))),
                          new(OpCodes.Ldfld, AccessTools.Field(typeof(GlobalWork), nameof(GlobalWork.u8GameMode))),
                          new(OpCodes.Ldc_I4_2),
                          new(OpCodes.Beq));

        start = matcher.Pos;
        var endLabel = (Label)matcher.Advance(4).Operand;
        end = matcher.MatchForward(false, [new() { labels = { endLabel } }]).Pos;

        // This patch lets you open the options menu by pressing SELECT.
        matcher
            .RemoveInstructionsInRange(start, end - 1)
            .Start()
            .Advance(start)
            .Insert(new(OpCodes.Ldarg_0),
                    new(OpCodes.Call, AccessTools.Method(typeof(OptionsMenu), nameof(PauseMenuPatchB))));

        return matcher.Instructions();
    }

    private static bool PauseMenuPatchA(PauseMenu self, int pIdx) {
        var ret = PauseMenuPatchAImpl(self, pIdx);
        if (ret) SoundController.Instance.Stop(SoundController.eChannel.SE, fadeTime: 0, bInstant: false);
        return ret;
    }

    private static bool PauseMenuPatchAImpl(PauseMenu self, int pIdx) {
        if (self._gameDebug?.DisplaingDebugMenu() == true)
            return false;

        if (self.gWork.u8Pause == 1)
            if (self.gWork.u8GameInfoMode == GAMEINFO_MODE.GAMEINFO_MODE_VS)
                if (self.gWork.u8PauseEntryPlayer != pIdx)
                    return false;

        if (self.sCheckPause() == 0)
            return false;

        var pad = self.input.Pad(self.gWork.player[pIdx].controllerNo);
        if (pad.IsDown(KeyMap.Start))
            return true;

        if (self.gWork.u8Pause == 1)
            if (pad.IsDown(KeyMap.B))
                return true;

        return false;
    }

    private static void PauseMenuPatchB(PauseMenu self) {
        if (self.isActiveDialogReturn) return;
        if (self.gWork.u8GameMode == GI_GMODE.GI_GMODE_TUTORIAL_B) return;

        var pad = self.input.Pad(0);
        if (pad.IsDown(KeyMap.Y)) {
            self.ShowDialog(true);
        } else if (pad.IsDown(KeyMap.Back)) {
            self.StartCoroutine(ShowOptionsMenu(self));
        }
    }

    private static System.Collections.IEnumerator ShowOptionsMenu(PauseMenu pauseMenu) {
        // TODO: Show a button prompt for entering the options menu
        // TODO: Actually changing settings on the fly doesn't really work yet.
        // A bunch of stuff is goign to need to be reloaded.

        yield return SceneManager.LoadSceneAsync("Option", LoadSceneMode.Additive);
        var optionsScene = SceneManager.GetSceneByName("Option");

        foreach (var obj in optionsScene.GetRootGameObjects()) {
            if (obj.GetComponent<KatamariPauseController>() is not KatamariPauseController kpc) continue;
            var fancyCursor = kpc.cursorImages[0];
            fancyCursor.transform.parent.GetChild(0).gameObject.SetActive(true);
            break;
        }

        var layer = new GameObject[] {
            pauseMenu.gameObject,
            pauseMenu.objKatamariCamera,
            pauseMenu.objGameCamera,
            pauseMenu.canvas.GetChild(0).gameObject,
        };

        foreach (var obj in layer) {
            obj.SetActive(false);
        }

        void onUnload(Scene scene) {
            if (scene == optionsScene) {
                foreach (var obj in layer) {
                    obj.SetActive(true);
                }
                SceneManager.sceneUnloaded -= onUnload;
            }
        }
        SceneManager.sceneUnloaded += onUnload;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Start))]
    private static void AddSettingsPrompt(PauseMenu __instance) {
        var guide = __instance.objGuidePause;
        var back = guide.transform.GetChild(0);

        var settings = Object.Instantiate(back);
        settings.name = "Text_Settings";

        Object.Destroy(settings.GetComponent<UITextLocalizer>());
        settings.GetComponent<Text>().text = "Settings";

        settings.SetParent(guide.transform, worldPositionStays: false);

        var ratio = 1600f / Camera.main.pixelWidth; // This doesn't seem like the right way to manage this, but it at least works properly.
        settings.Translate(-25 * ratio, 0, 0);
        back.transform.Translate(5 * ratio, 0, 0);

        var glyph = settings.GetChild(0).GetComponent<KeyImageCheck>();
        glyph.iconKeyType = KeyMap.Back;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), nameof(Player.InputMoveTypeUpdate))]
    public static void ActuallyUpdateMoveType(Player __instance) {
        // The vanilla game's copy of this method is just... unfinished?
        __instance.inputMoveType = (GlobalWork.eMoveType)GlobalWork.Instance.moveType[__instance.playerNo];
    }

    // If a coroutine yields a WaitForSeconds while the game is paused,
    // the coroutine freezes indefinitely, because Time.timeScale is set to 0.
    //
    // In the vanilla game this isn't an issue, because the options are only usable from the Home Planet.
    // WaitForSecondsRealtime was the correct thing for Monkeycraft to use anyway,
    // since this is fiddling with graphics and shouldn't depend on time scale.
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(QualityManager), nameof(QualityManager.ISetup), MethodType.Enumerator)]
    public static IEnumerable<CodeInstruction> FixSoftlock(IEnumerable<CodeInstruction> instructions) {
        var wfs = AccessTools.Constructor(typeof(WaitForSeconds), [typeof(float)]);
        var wfsr = AccessTools.Constructor(typeof(WaitForSecondsRealtime), [typeof(float)]);

        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Newobj, wfs))
            .Repeat(cm => cm.SetOperandAndAdvance(wfsr))
            .Instructions();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.ChangeIndex))]
    public static void ApplySettings() {
        var gWork = GlobalWork.Instance;
        var game = gWork.manGame;
        if (game == null) return;

        var setup = game.GetComponent<SetupRenderTexture>();

        setup.Release();
        setup.Awake();
        setup.setIndex = 1;
    }

    // lmao the original code uses == where it should use !=:
    // checking if gWork.renderTexture is null to decide whether to iterate through it
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(SetupRenderTexture), nameof(SetupRenderTexture.Release))]
    public static IEnumerable<CodeInstruction> FixRelease(IEnumerable<CodeInstruction> instructions) {
        return new CodeMatcher(instructions)
            .MatchForward(false, [new(OpCodes.Brtrue)])
            .SetOpcodeAndAdvance(OpCodes.Brfalse)
            .Instructions();
    }

    private static readonly string QualitySettingsPath = Path.Combine(FileManager.SaveTemporaryPath, "Setting/quality.ex.setting");

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Load))]
    public static bool LoadExtraSettings(QualitySetting __instance) {
        if (!File.Exists(QualitySettingsPath)) return true;

        try {
            var status = File.ReadAllText(QualitySettingsPath)
                .Split(',')
                .Select(int.Parse)
                .ToList();

            while (status.Count < __instance.ItemMax) status.Add(0);

            if (__instance.statusNo[0] >= __instance.itemName.Count) __instance.statusNo[0] = 0;
            if (__instance.statusNo[4] >= QualitySetting.antialiasingValue.Length) __instance.statusNo[4] = 1;

            return false;
        } catch {
            return true;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Load))]
    public static void PadSettings(QualitySetting __instance) {
        while (__instance.statusNo.Length < __instance.ItemMax) {
            __instance.statusNo = [.. __instance.statusNo, 0];
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Save))]
    public static bool SaveExtraSettings(QualitySetting __instance) {
        var path = Path.Combine(FileManager.SaveTemporaryPath, "Setting/quality.ex.setting");
        var text = __instance.statusNo.Join(n => n.ToString(), ",");
        File.WriteAllText(path, text);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(QualitySetting), MethodType.Constructor)]
    public static void CreateExtraSettings(QualitySetting __instance) {
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), MethodType.Constructor)]
    public static void ExposeExtraSettings(QualitySetting __instance) {
        var items = __instance.itemName;
        items.Add(["16x", "1x", "2x", "4x", "8x"]);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UguiUtility), nameof(UguiUtility.Awake))]
    public static void ExtendSettingsUI(UguiUtility __instance) {
        if (__instance.gameObject.scene.name != "Quality") return;

        Transform get(string name) => __instance.transform.Find(name);

        var labelPre = get("TextItem7");
        var valuePre = get("TextItemExp7");
        var leftPre = get("Button7L");
        var rightPre = get("Button7R");

        // Text item 6 comes after 7 in the vanilla layout, and it's the disabled DoF option
        // TODO: Consider just instantiating my own setting for the DoF? The vanilla one, being disabled, is a little inconsistent
        // TODO: Off/Low/Med/High DoF options
        // TODO: Actually honor the DoF and AF settings rather than just giving them a UI

        get("TextItem6").gameObject.SetActive(true);
        get("TextItemExp6").gameObject.SetActive(true);
        get("Button6L").gameObject.SetActive(true);
        get("Button6R").gameObject.SetActive(true);

        var spacing = get("TextItem1").position.y - get("TextItem0").position.y;

        var prevFocus = get("TextItem6");
        LinkFocus(get("TextItem7"), prevFocus);

        for (var i = 8; i < QualitySetting.Instance.ItemMax; i++) {
            var offset = new Vector2(0, spacing * (i - 6));

            var label = Object.Instantiate(labelPre, labelPre.parent); // TODO: I didn't know this overload exists and should use it in more places.
            label.name = $"TextItem{i}";
            label.transform.Translate(offset);
            var value = Object.Instantiate(valuePre, valuePre.parent);
            value.name = $"TextItemExp{i}";
            value.transform.Translate(offset);

            var left = Object.Instantiate(leftPre, leftPre.parent);
            left.name = $"Button{i}L";
            left.transform.Translate(offset);

            var right = Object.Instantiate(rightPre, rightPre.parent);
            right.name = $"Button{i}R";
            right.transform.Translate(offset);

            LinkFocus(prevFocus, label);
            prevFocus = label;
            label.GetComponent<UguiFocus>().focusID = i;

            label.GetComponent<UITextLocalizer>().textID = i switch {
                8 => "UI_SYS_213",
                _ => "UI_TTR_022", // Placeholder
            };
        }

        LinkFocus(prevFocus, get("TextItem0"));
    }

    private static void LinkFocus(Component prev, Component next) {
        var prevF = prev.GetComponent<UguiFocus>();
        var nextF = next.GetComponent<UguiFocus>();
        prevF.downKeyMove = nextF;
        nextF.upKeyMove = prevF;
    }

    // TODO: Update quality settings mid-level, e.g. ambient occlusion
}
