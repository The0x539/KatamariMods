using DefineEnum;

using HarmonyLib;

using MyGame;
using MyGame.InputStatus;

using System.Linq;
using System.Reflection.Emit;

using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DevUtils;

// Make it possible to access the options menu during a level.

static class IngameOptions {
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
    public static IL AddGuy(IL il) {
        return new CodeMatcher(il)
            //////// This patch lets you exit the normal pause menu by pressing B, in addition to the usual START.
            .MatchForward(false,
                          // Locate the first instance of checking whether START is pressed.
                          new(OpCodes.Ldc_I4_7),
                          new(OpCodes.Callvirt, Member.Method<InputPadBase>(ipb => ipb.IsDown(KeyMap.A))))
            .MatchBack(false,
                       // Backtrack to the most recent access to `this.input`, which is earlier in the same line of C#.
                       new(OpCodes.Ldarg_0),
                       new(OpCodes.Ldfld, Member.Field<PauseMenu>(pm => pm.input)))
            .GetPos(out var start)
            // Find the end of this if-statement, which sets a local variable to 1.
            .MatchForward(false, [new(OpCodes.Stloc_S)])
            .GetPos(out var end)
            // Grab a handle to that local variable, then remove the entire code block.
            .GetOperand(out var flagLocalVar)
            .RemoveInstructionsInRange(start, end)
            // Replace the code block with a call to our replacement function.
            .Start()
            .Advance(start)
            .Insert(new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldloc_3),
                    new(OpCodes.Call, Member.Method((PauseMenu pm, int pIdx) => PauseMenuPatchA(pm, pIdx))),
                    new(OpCodes.Stloc_S, flagLocalVar))
            //////// This patch lets you open the options menu by pressing X/square.
            .MatchForward(false,
                          // Find: if (this.gWork.u8GameMode != GI_GMODE.TUTORIAL_B) {}
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, Member.Field<PauseMenu>(pm => pm.gWork)),
                          new(OpCodes.Ldfld, Member.Field<GlobalWork>(gw => gw.u8GameMode)),
                          new(OpCodes.Ldc_I4_2),
                          new(OpCodes.Beq))
            // Grab the {
            .GetPos(out start)
            // Grab the }
            .Advance(4)
            .GetOperand(out Label endLabel)
            // Find the actual location of the }
            .MatchForward(false, [new() { labels = { endLabel } }])
            .GetPos(out end)
            // Remove the entire block
            .RemoveInstructionsInRange(start, end - 1)
            // Replace it with a call to our function
            .Start()
            .Advance(start)
            .Insert(new(OpCodes.Ldarg_0),
                    new(OpCodes.Call, Member.Method((PauseMenu pm) => PauseMenuPatchB(pm))))
            .Instructions();
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

        var pad = self.input.Pad(0);
        if (pad.IsDown(KeyMap.Y) && self.gWork.u8GameMode is not (GI_GMODE.GI_GMODE_TUTORIAL or GI_GMODE.GI_GMODE_TUTORIAL_B)) {
            self.ShowDialog(true);
        } else if (pad.IsDown(KeyMap.X)) {
            self.StartCoroutine(ShowOptionsMenu(self));
        }
    }

    private static System.Collections.IEnumerator ShowOptionsMenu(PauseMenu pauseMenu) {
        var optionsScene = SceneManager.GetSceneByName("Option");
        if (optionsScene.isLoaded) yield break;
        yield return SceneManager.LoadSceneAsync("Option", LoadSceneMode.Additive);
        if (!optionsScene.IsValid()) optionsScene = SceneManager.GetSceneByName("Option"); // ???

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

        var settings = UnityObject.Instantiate(back, guide.transform);
        settings.name = "Text_Settings";

        UnityObject.Destroy(settings.GetComponent<UITextLocalizer>());
        settings.GetComponent<Text>().text = "Settings";

        var ratio = 1600f / guide.transform.parent.GetComponent<Canvas>().pixelRect.width; // This doesn't seem like the right way to manage this, but it at least works properly.
        settings.Translate(-25 * ratio, 0, 0);
        back.transform.Translate(5 * ratio, 0, 0);

        var glyph = settings.GetChild(0).GetComponent<KeyImageCheck>();
        glyph.iconKeyType = glyph.iconKeyTypeWork = KeyMap.X;
        // Trigger change detection
        glyph.onOff = false;
        glyph.OnOff = true;
    }

    // Since we made it possible to use the B button to exit the menu,
    // show that button instead of Start for visual consistency.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Start))]
    private static void UseEastButtonForExitPrompt(PauseMenu __instance) {
        var guide = __instance.objGuidePause;
        var glyph = guide.transform.Find("Text_Back/Image_icon").GetComponent<KeyImageCheck>();
        glyph.iconKeyType = glyph.iconKeyTypeWork = KeyMap.B;
        // Trigger change detection
        glyph.onOff = false;
        glyph.OnOff = true;
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
    public static IL FixSoftlock(IL il) {
        var wfs = Member.Constructor(() => new WaitForSeconds(0f));
        var wfsr = Member.Constructor(() => new WaitForSecondsRealtime(0f));

        return new CodeMatcher(il)
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
    public static IL FixRelease(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false, [new(OpCodes.Brtrue)])
            .SetOpcodeAndAdvance(OpCodes.Brfalse)
            .Instructions();
    }

    private static GameObject GetPauseBackdrop()
        => SceneManager.GetSceneByName("UI_Pause")
            .GetRootGameObjects()
            .First(o => o.name == "UI")
            .transform
            .GetChild(1)
            .GetChild(0)
            .gameObject;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualityManager), nameof(QualityManager.Start))]
    public static void HideBackdrop(QualityManager __instance) {
        if (!SceneManager.GetSceneByName("GameMain").isLoaded) return;

        __instance.transform.Find("Canvas/RawImage").gameObject.SetActive(false);
        GetPauseBackdrop().SetActive(false);

        // This camera would otherwise render for 1 frame, and it looks like a glitch.
        foreach (var camera in Camera.allCameras) {
            if (camera.gameObject.scene == __instance.gameObject.scene) {
                camera.enabled = false;
                break;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualityManager), nameof(QualityManager.Close))]
    public static void RestoreBackdrop() {
        if (!SceneManager.GetSceneByName("GameMain").isLoaded) return;

        GetPauseBackdrop().SetActive(true);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(QualitySetting), nameof(QualitySetting.Set))]
    public static void ActuallyUpdateSettings(QualitySetting __instance) {
        if (!SceneManager.GetSceneByName("GameMain").isLoaded) return;

        var camera = Camera.main;
        if (camera.GetComponent<PostProcessingBehaviour>() is PostProcessingBehaviour ppb) {
            ppb.profile.ambientOcclusion.enabled = __instance.IsSsao;
            ppb.profile.vignette.enabled = __instance.IsVignette;
            // This one should only be set if the graphics mod is installed.
            // The easiest way to implement that is to just... do this one in that mod instead.
            //ppb.profile.depthOfField.enabled = __instance.IsDOF;
        }
    }

    /*
    [HarmonyPostfix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.PauseState))]
    public static void OpenSoundMenu() {
        var pad = InputController.Instance.Pad(0);
        if (pad.IsDown(KeyMap.R1)) {
            // Whoops, never mind.
            // The sound settings aren't a separate scene.
            // They're part of UI_OujiStar.
            // Oh well. Not the end of the world.
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KatamariPauseController), nameof(KatamariPauseController.Start))]
    private static void AddSoundPrompt(KatamariPauseController __instance) {
        var graphics = __instance.transform.Find("Canvas/MainMenuPC/RawImage3").gameObject;
        var sound = UnityObject.Instantiate(graphics, graphics.transform.parent);
        sound.name = "RawImage4_Sound";
        sound.GetComponent<RectTransform>().Translate(new(-180, 0));
        sound.GetComponentInChildren<UITextLocalizer>().textID = "OT_CTG_014";
        sound.GetComponentInChildren<KeyImageCheck>().iconKeyType = KeyMap.R1;
    }
    */
}
