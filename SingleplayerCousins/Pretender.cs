using BepInEx.Configuration;

using DefineEnum;

using HarmonyLib;

using SingleplayerCousins.Cousins;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

using Json = SimpleJson.SimpleJson;

namespace SingleplayerCousins;

public sealed class Pretender {
    public static readonly Dictionary<int, Pretender> pretenders;
    private static int maxID = 24;

    public static int MaxID => maxID;

    private const string extension = "glb";

    static Pretender() {
        pretenders = [];

        const string dirPath = "./Pretenders/";
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
        var items = Directory.GetFiles(dirPath);
        Array.Sort(items);

        var dynamicId = (int)PretenderId.DYNAMIC;
        foreach (var path in items) {
            if (Path.GetExtension(path) != $".{extension}") continue;
            if (path.EndsWith($".ball.{extension}")) continue;

            int id;

            var name = Path.GetFileNameWithoutExtension(path);
            try {
                var known = (PretenderId)Enum.Parse(typeof(PretenderId), name);
                id = (int)known;
            } catch {
                id = dynamicId++;
            }

            Register(id, name, path);
        }

        Register((int)PretenderId.Vanta, "Vanta", "");
    }

    private static void Register(int id, string name, string path) {
        maxID = Math.Max(maxID, id);

        var ballPath = path.Replace($".{extension}", $".ball.{extension}");
        if (!File.Exists(ballPath)) ballPath = null;

        var p = new Pretender { Id = id, Name = name, FilePath = path, BallFilePath = ballPath };
        pretenders.Add(id, p);

        Plugin.logger.LogInfo($"Registered pretender: {name} (ID: {id})");
    }

    public int Id { get; init; } = 0;
    public string Name { get; init; } = "";
    public string FilePath { get; init; } = "";
    public string? BallFilePath { get; init; } = null;

    public bool HasBall => this.BallFilePath != null || this.Id == (int)PretenderId.Vanta;

    public GameObject Reify() {
        var name = "OUJI16"; // June is a Prince-shaped character who's also unlocked from the start, so a good candidate
        var ouji = AssetBundleSimulator.Instance.LoadAsset<GameObject>(name, name);
        ouji.name = $"OUJI{this.Id:00}-{this.Name}";

        if (this.Id != (int)PretenderId.Vanta) {
            PretenderLoader.ApplyModel(ouji, this.FilePath);
        }

        switch ((PretenderId)this.Id) {
            case PretenderId.Dega:
                ouji.AddComponent<Dega>();
                break;
            case PretenderId.Soyo:
                var handFix = ouji.AddComponent<HandFix>();
                handFix.handAngle = new(0, 0, 20);
                // okay it's kinda bad that the camera counter-angle messes with these axes,
                // but seriously why is the held camera not just parented to the hand in the original player model?
                handFix.cameraPosition = new(0.13f, 0, 0.1f);
                handFix.cameraCounterAngle = new(-25, 0, 90);
                handFix.cameraScale = 1;
                break;
        }

        Plugin.logger.LogInfo($"Successfully loaded pretender: {ouji.name}");
        return ouji;
    }

    public GameObject ReifyBall() {
        var name = "core_01";
        var ball = AssetBundleSimulator.Instance.LoadAsset<GameObject>(name, name);

        if (this.Id == (int)PretenderId.Vanta) {
            Vanta.DressBall(ball);
        } else {
            PretenderLoader.ApplyBallModel(ball, this.BallFilePath ?? "");
        }

        return ball;
    }
}

public static class PretenderPatches {
    private static readonly MethodInfo loadGameObject = Member.Method<AssetBundleSimulator>(s => s.LoadAsset<GameObject>("", ""));
    private static readonly MethodInfo assetBundleSimulatorInstance = Member.Getter(() => AssetBundleSimulator.Instance);

    private static readonly ConfigEntry<string> entryPretenderKatamariLevels = Plugin.configFile.Bind(
        "Pretenders",
        "PretenderKatamariLevels",
        "2, Moon",
        new ConfigDescription(
            "Levels to use custom katamari skins in. Possible values (comma-separated): \n" +
             "1, 2, 3, 4, 5, 6, 7, 8, 9, Moon, " +
             "Cancer, Cygnus, Corona Borealis, Pisces, Virgo, Gemini, " +
             "Ursa Major, Taurus, Polaris"
        )
    );

    private static HashSet<GAMEINFO_MIS> ParsePklConfig(string pklConfig) {
        var ret = new HashSet<GAMEINFO_MIS>();
        foreach (var word in pklConfig.Split(',')) {
            int mission = word.Trim().ToLower() switch {
                "1" => (int)GAMEINFO_MIS.GAMEINFO_MIS_01,
                "2" => (int)GAMEINFO_MIS.GAMEINFO_MIS_02,
                "3" => (int)GAMEINFO_MIS.GAMEINFO_MIS_04, // missions 3 and 4 are swapped in the game data
                "4" => (int)GAMEINFO_MIS.GAMEINFO_MIS_03,
                "5" => (int)GAMEINFO_MIS.GAMEINFO_MIS_05,
                "6" => (int)GAMEINFO_MIS.GAMEINFO_MIS_06,
                "7" => (int)GAMEINFO_MIS.GAMEINFO_MIS_07,
                "8" => (int)GAMEINFO_MIS.GAMEINFO_MIS_08,
                "9" => (int)GAMEINFO_MIS.GAMEINFO_MIS_09,
                "10" or "moon" => Define.GAMEINFO_MIS_MOON,
                "cancer" => Define.GAMEINFO_M_CANCER,
                "cygnus" => Define.GAMEINFO_M_CYGNUS,
                "corona borealis" => Define.GAMEINFO_M_CORONA_BOREALIS,
                "pisces" => Define.GAMEINFO_M_PISCES,
                "virgo" => Define.GAMEINFO_M_VIRGO,
                "gemini" => Define.GAMEINFO_M_GEMINI,
                "ursa major" => Define.GAMEINFO_M_URSA_MAJOR,
                "taurus" => Define.GAMEINFO_M_TAURUS,
                "polaris" => Define.GAMEINFO_M_POLARIS,
                _ => -1,
            };
            if (mission == -1) {
                Console.WriteLine($"Warning: Unknown mission `{word.Trim()}`. Ignoring.");
                continue;
            }
            ret.Add((GAMEINFO_MIS)mission);
        }
        return ret;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GlobalWork), nameof(GlobalWork.InitItoko))]
    public static void InitItoko(GlobalWork __instance) {
        if (__instance.objItoko.Length > 24) return;

        var newArr = new GameObject[Pretender.MaxID];

        for (var i = 0; i < 24; i++) {
            newArr[i] = __instance.objItoko[i];
        }
        foreach (var j in Pretender.pretenders.Keys) {
            newArr[j - 1] = Pretender.pretenders[j].Reify();
        }

        __instance.objItoko = newArr;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(KinokoRotator), nameof(KinokoRotator.CloneItoko))]
    public static void DressVantaOnMushroom(KinokoRotator __instance) {
        foreach (var ouji in __instance._list_dataOuji) {
            if (ouji.kinokoMover.OujiIndex == (int)PretenderId.Vanta) {
                Vanta.Dress(ouji.objOuji);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Load))]
    public static void AddToOujiArray(ref SaveManager2.SaveData saveData) => AddToOujiArray(saveData.siGame);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Save))]
    public static void AddToOujiArray(GlobalWork gWork) => AddToOujiArray(gWork.siGame);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SelectManager), nameof(SelectManager.gTj_ResultInit))]
    public static void AddToOujiArray(SelectManager __instance) => AddToOujiArray(__instance.gWork.siGame);

    public static void AddToOujiArray(SI_GAME siGame) {
        if (siGame.oujiArray.Length > 24) return;

        var newArr = new int[Pretender.MaxID];

        for (var i = 0; i < 24; i++) {
            newArr[i] = siGame.oujiArray[i];
        }
        foreach (var j in Pretender.pretenders.Keys) {
            newArr[j - 1] = 1;
        }

        siGame.oujiArray = newArr;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Load))]
    public static void ObscureFromSaveFile(ref SaveManager2.SaveData saveData) => ObscureFromSaveFile(saveData.siGame);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SaveManager2), nameof(SaveManager2.Save))]
    public static void ObscureFromSaveFile(GlobalWork gWork) => ObscureFromSaveFile(gWork.siGame);

    public static void ObscureFromSaveFile(SI_GAME siGame) {
        if (siGame.oujiArray.Length <= 24) return;

        siGame.oujiArray = [.. siGame.oujiArray.Take(24)];
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIKinoko), nameof(UIKinoko.SetOujiName))]
    public static void SetName(UIKinoko __instance, int id, bool isOnePlayer) {
        if (!Pretender.pretenders.TryGetValue(id, out var pretender)) return;

        var text = isOnePlayer ? __instance._text_playerLName : __instance._text_playerRName;
        text.text = $"・{pretender.Name}・";
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static IL LoadIngamePlayer(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Call, assetBundleSimulatorInstance),
                          new(OpCodes.Ldarg_0), new(OpCodes.Ldfld),
                          new(OpCodes.Ldarg_0), new(OpCodes.Ldfld),
                          new(OpCodes.Callvirt, loadGameObject))
            .RemoveInstructions(6)
            .InsertAndAdvance(new(OpCodes.Ldarg_0),
                              new(OpCodes.Call, Member.Method((Player p) => LoadIngameImpl(p))))
            .Instructions();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CharacterCloneController), nameof(CharacterCloneController.Update))]
    public static IL LoadIngameClone(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Call, assetBundleSimulatorInstance),
                          new(OpCodes.Ldarg_0), new(OpCodes.Ldfld), new(OpCodes.Ldfld),
                          new(OpCodes.Ldarg_0), new(OpCodes.Ldfld), new(OpCodes.Ldfld),
                          new(OpCodes.Callvirt, loadGameObject))
            .SetOpcodeAndAdvance(OpCodes.Nop).RemoveInstructions(7)
            .InsertAndAdvance(new(OpCodes.Ldarg_0),
                              new(OpCodes.Call, Member.Method((CharacterCloneController c) => LoadIngameImpl(c))))
            .Instructions();
    }

    private static GameObject LoadIngameImpl(CharacterCloneController clone) => LoadIngameImpl(clone.player);

    private static GameObject LoadIngameImpl(Player player) {
        if (Pretender.pretenders.TryGetValue(player.oujiNo, out var pretender)) {
            // Fix the animator controller situation
            var newArr = new RuntimeAnimatorController[player.oujiNo + 1];
            int i;
            for (i = 0; i < player.animController.Length; i++) {
                newArr[i] = player.animController[i];
            }
            for (; i < newArr.Length; i++) {
                newArr[i] = player.animController[0];
            }
            player.animController = newArr;

            var ouji = pretender.Reify();
            if (pretender.Id == (int)PretenderId.Vanta) {
                Vanta.Dress(ouji);
            }
            return ouji;
        } else {
            var name = player.oujiName;
            return AssetBundleSimulator.Instance.LoadAsset<GameObject>(name, name);
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Player), nameof(Player.Start))]
    public static IL ChooseCore(IL il) {
        return new CodeMatcher(il)
            .MatchForward(false,
                          new(OpCodes.Ldarg_0),
                          new(OpCodes.Ldfld, Member.Field<Player>(p => p.gWork)),
                          new(OpCodes.Ldfld, Member.Field<GlobalWork>(gw => gw.playMission)))
            .GetPos(out var start)
            .MatchForward(false, [new(OpCodes.Callvirt, loadGameObject)])
            .GetPos(out var end)
            .Start()
            .Advance(start + 1)
            .RemoveInstructionsInRange(start + 1, end)
            .InsertAndAdvance([new(OpCodes.Call, Member.Method((Player p) => ChooseCoreImpl(p)))])
            .MatchForward(false,
                          new(OpCodes.Callvirt, Member.Getter<Renderer>(r => r.sharedMaterial)),
                          new(OpCodes.Callvirt, Member.Setter<Renderer>(r => r.sharedMaterial)))
            .SetOperandAndAdvance(Member.Getter<Renderer>(r => r.sharedMaterials))
            .SetOperandAndAdvance(Member.Setter<Renderer>(r => r.sharedMaterials))
            .Instructions();
    }

    // Copied verbatim from the game.
    private static readonly int[] oujiCores = [
        0, 1, 11, 22, 24, 3, 9, 21, 2, 14,
        8, 12, 5, 15, 20, 18, 23, 16, 13, 17,
        10, 19, 6, 4, 7
    ];

    private static bool ShouldUseOujiCore(Player p) {
        var gw = p.gWork;
        if (gw.u8GameInfoMode == GAMEINFO_MODE.GAMEINFO_MODE_VS) return true;
        if (gw.u8GameType == GI_GAMETYPE.GI_GAMETYPE_X) return true;

        if (p.oujiNo == 1) return false; // When playing as the Prince, match vanilla behavior only

        Plugin.configFile.Reload();
        var pklConfig = entryPretenderKatamariLevels.Value;
        if (pklConfig.ToLower().Contains("all") || pklConfig.Contains("*")) return true;
        var missions = ParsePklConfig(pklConfig);
        if (missions.Contains(gw.playMission)) return true;

        return false;
    }

    public static GameObject ChooseCoreImpl(Player p) {
        var i = (int)p.gWork.playMission;
        if (ShouldUseOujiCore(p)) {
            if (p.oujiNo < oujiCores.Length) {
                i = oujiCores[p.oujiNo];
            } else if (Pretender.pretenders.TryGetValue(p.oujiNo, out var pretender)) {
                if (pretender.HasBall) {
                    return pretender.ReifyBall();
                }
            }
        }
        var name = $"core_{i:D2}";
        return AssetBundleSimulator.instance.LoadAsset<GameObject>(name, name);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Entity_king_text), nameof(Entity_king_text.GetLocaliseText))]
    public static void OverrideLocalization(string id, int local, ref string __result) {
        if (local != (int)LANGUAGE.ENGLISH) return;

        if (Plugin.OujiId == (int)PretenderId.Dega) {
            __result = id switch {
                "OT_OBJ_0512" => "Bud",
                "OT_OBJ_1365" => "Bodega, Japan",
                "OT_OBJ_1348" => "Bodega Sign",
                "UI_SYS_002" => __result.Replace("Kata", "Rrata"),
                _ => __result,
            };
        }
    }
}

internal static class PretenderLoader {
    internal static readonly Assimp.AssimpContext ctx;

    static PretenderLoader() {
        var platformHelper = Type.GetType("Assimp.Unmanaged.PlatformHelper, AssimpNet, Version=4.1.0.0, Culture=neutral, PublicKeyToken=0d51b391f59f42a6");
        var targetMethod = AccessTools.Method(platformHelper, "GetAppBaseDirectory");
        var patchMethod = AccessTools.Method(typeof(PretenderLoader), nameof(StupidHack));
        new Harmony("Stupid hack").Patch(targetMethod, postfix: new(patchMethod));
        ctx = new();
    }

    private static void StupidHack(ref string __result) => __result = "./katamari_Data/Plugins";

    private static readonly Dictionary<string, Assimp.Scene> sceneCache = new();

    public static Assimp.Scene LoadFbx(string path) {
        if (sceneCache.TryGetValue(path, out var existing)) return existing;
        var scene = ctx.ImportFile(path);
        sceneCache[path] = scene;
        return scene;
    }

    public static void ApplyModel(GameObject ouji, string path) {
        if (path.EndsWith(".glb")) {
            Gltf.UnityJsonSerializerStrategy.Register();
            // TODO: cache the JSON and binary like with FBX
            var file = File.OpenRead(path);
            var reader = new BinaryReader(file);
            Gltf.Glb.Parse(reader, out var json, out var binary);
            var asset = Json.DeserializeObject<Gltf.AssetFile>(json)!;
            ApplyModel(ouji, asset, binary);
        } else {
            ApplyModel(ouji, LoadFbx(path));
        }
    }

    public static void ApplyBallModel(GameObject ball, string path) => ApplyBallModel(ball, LoadFbx(path));

    private sealed class ImportMetadata {
        public bool preventArmatureExplosion = false;
        public Vector3 scale = Vector3.one;
        public Vector3 position = Vector3.zero;
        // Off by default because Soyo and Dega, the first two characters I was serious about adding,
        // both use pixel art textures, which Unity's lack of anisotropic point filtering.
        // As demonstrated with my work on terrain textures, this produces awful results,
        // so it's preferable to just disable mipmaps altogether for pixel art character textures.
        public bool mipmap = false;
    }

    public static void ApplyModel(GameObject ouji, Assimp.Scene scene) {
        var bodyParts = ouji.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
            .ToDictionary(x => x.gameObject.name);

        var bones = new Dictionary<string, Transform>();
        foreach (var bone in ouji.GetComponentsInChildren<Transform>(true)) {
            bones[bone.name] = bone;
        }

        var body_m = bodyParts["body_m"];
        var body_root = body_m.transform.parent;

        var metadata = new ImportMetadata();

        var nodes = new Stack<Assimp.Node>();
        var newBones = new List<Assimp.Node>();
        nodes.Push(scene.RootNode);
        while (nodes.Count > 0) {
            var node = nodes.Pop();
            if (bones.TryGetValue(node.Name, out var bone)) {
                node.Transform.Decompose(out var scale, out var rotation, out var position);
                bone.transform.localPosition = position.ToUnity();
                bone.transform.localRotation = rotation.ToUnity();
                bone.transform.localScale = scale.ToUnity();
            } else if (node.Name.StartsWith("JNT_")) {
                newBones.Add(node);
            }

            if (node.Parent?.Name == "METADATA") {
                switch (node.Name.ToLower()) {
                    case "prevent armature explosion":
                        metadata.preventArmatureExplosion = true;
                        break;
                    case "adjustment":
                        node.Transform.Decompose(out var scale, out _, out var position);
                        metadata.position = position.ToUnity() / 100;
                        metadata.scale = scale.ToUnity();
                        if (scale.X != scale.Y || scale.Y != scale.Z) {
                            Plugin.logger.LogWarning($"Pretender {ouji.name} has a non-uniform scale adjustment. Import results may be dubious.");
                        }
                        if (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0) {
                            Plugin.logger.LogWarning($"Pretender {ouji.name} has a non-positive scale adjustment. Import results may be dubious.");
                        }
                        break;
                    case "mipmap":
                        metadata.mipmap = true;
                        break;
                }
            }

            foreach (var child in node.Children) {
                nodes.Push(child);
            }
        }

        foreach (var node in newBones) {
            var bone = new GameObject(node.Name);
            // This hideFlags is load-bearing for e.g. Dega's tail
            bone.hideFlags = HideFlags.HideAndDontSave;
            bones.Add(bone.name, bone.transform);
        }
        foreach (var node in newBones) {
            var bone = bones[node.Name];
            bone.SetParent(bones[node.Parent.Name]);
            node.Transform.Decompose(out var scale, out var rotation, out var position);
            bone.transform.localPosition = position.ToUnity();
            bone.transform.localRotation = rotation.ToUnity();
            bone.transform.localScale = scale.ToUnity();
        }

        var uMaterials = new List<Material?>();
        foreach (var aMat in scene.Materials) {
            Texture2D uTex;
            try {
                uTex = LoadTexture(scene, aMat, metadata.mipmap);
            } catch (Exception ex) {
                Console.WriteLine($"Texture {aMat.Name} has no filepath: {ex}");
                uMaterials.Add(null);
                continue;
            }

            var uMat = UnityObject.Instantiate(body_m.material);
            if (aMat.Name == "FaceTexture") {
                uMat.EnableKeyword("_ALPHATEST_ON");
                uMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            // This name is absolutely not guaranteed to be unique across different characters.
            // Not sure how much of an issue that's going to be.
            uMat.name = uTex.name = aMat.Name;
            uMat.mainTexture = uTex;
            uMaterials.Add(uMat);
        }

        foreach (var aMesh in scene.Meshes) {
            var bodyPart = new GameObject(aMesh.Name);
            bodyPart.transform.SetParent(body_root);
            // This hideFlags is load-bearing for all custom-model pretenders (everyone except Vanta at the time of writing)
            bodyPart.hideFlags = HideFlags.HideAndDontSave;

            var renderer = bodyPart.AddComponent<SkinnedMeshRenderer>();
            var uMesh = new Mesh() { name = aMesh.Name };

            LoadVertices(aMesh, uMesh);
            LoadIndices(aMesh, uMesh);
            LoadUVs(aMesh, uMesh);

            var uBoneWeights = new BoneWeight[aMesh.VertexCount];
            var uBindposes = new List<Matrix4x4> { Matrix4x4.identity };
            var uBones = new List<Transform> { bones["JNT_root"] };

            foreach (var aBone in aMesh.Bones) {
                int boneIdx;
                if (bones.TryGetValue(aBone.Name, out var bone)) {
                    boneIdx = uBones.Count;
                    uBones.Add(bone);
                    uBindposes.Add(aBone.OffsetMatrix.ToUnity());
                } else {
                    boneIdx = 0;
                }

                foreach (var aWeight in aBone.VertexWeights) {
                    if (aWeight.Weight == 0) continue;
                    uBoneWeights[aWeight.VertexID] = uBoneWeights[aWeight.VertexID].AddWeight(boneIdx, aWeight.Weight);
                }
            }

            renderer.bones = uBones.ToArray();
            uMesh.bindposes = uBindposes.ToArray();
            uMesh.boneWeights = uBoneWeights;

            LoadNormals(aMesh, uMesh);

            uMesh.UploadMeshData(markNoLongerReadable: true);

            renderer.rootBone = bones["JNT_root"];
            renderer.sharedMesh = uMesh;
            if (uMaterials[aMesh.MaterialIndex] is Material mat) {
                renderer.sharedMaterial = mat;
            }

            foreach (var groupName in new[] { "eye", "face", "mouth", "parts" }) {
                if (!aMesh.Name.StartsWith(groupName + "[")) continue;

                var chop1 = aMesh.Name.Substring(groupName.Length + 1);
                var chop2 = chop1.Substring(0, chop1.IndexOf(']'));
                var idx = int.Parse(chop2) - 1;

                var parent = ouji.transform.Find("face_root/" + groupName);

                var replaced = parent.GetChild(idx);
                replaced.SetAsLastSibling();
                UnityObject.Destroy(replaced.gameObject);

                renderer.transform.SetParent(parent);
                renderer.transform.SetSiblingIndex(idx);
                renderer.gameObject.SetActive(false);

                break;
            }
        }

        foreach (var part in bodyParts.Values) {
            part.enabled = false;
        }
        bones["JNT_antenna"].GetChild(0).gameObject.SetActive(false);

        if (metadata.position != Vector3.zero || metadata.scale != Vector3.one) {
            var root = bones["JNT_root"];
            root.transform.localPosition = Vector3.Scale(metadata.position, metadata.scale);
            root.transform.localScale = metadata.scale;
        }

        if (metadata.preventArmatureExplosion) {
            ouji.AddComponent<PreventArmatureExplosion>();
        }
    }

    public static void ApplyModel(GameObject ouji, Gltf.AssetFile file, byte[] binary) {
        if (file.scene is not uint iScene) {
            throw new Exception("glTF file has no default scene!");
        }

        var bodyParts = ouji.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true)
            .ToDictionary(c => c.gameObject.name);

        var bones = ouji.GetComponentsInChildren<Transform>(includeInactive: true)
            .ToDictionary(c => c.gameObject.name);

        var body_m = bodyParts["body_m"];
        var body_root = body_m.transform.parent;

        var nodes = new Stack<Gltf.Node>();
        foreach (var iRoot in file.scenes[iScene].nodes) {
            nodes.Push(file.nodes[iRoot]);
        }

        var metadata = new ImportMetadata();
        var newBones = new List<Gltf.Node>();
        var meshNodes = new List<Gltf.Node>();
        var parents = new Dictionary<Gltf.Node, Gltf.Node>();
        while (nodes.Count > 0) {
            var node = nodes.Pop();
            if (node.name == null) continue;
            if (bones.TryGetValue(node.name, out var bone)) {
                bone.localPosition = node.translation ?? Vector3.zero;
                bone.localRotation = node.rotation ?? Quaternion.identity;
                bone.localScale = node.scale ?? Vector3.one;
            } else if (node.name.StartsWith("JNT_")) {
                newBones.Add(node);
            }

            if (node.mesh != null) {
                meshNodes.Add(node);
            }

            if (node.name != "METADATA") {
                foreach (var iChild in node.children) {
                    var child = file.nodes[iChild];
                    parents.Add(child, node);
                    nodes.Push(child);
                }
                continue;
            }

            foreach (var iChild in node.children) {
                var child = file.nodes[iChild];
                switch (child.name?.ToLower()) {
                    case "prevent armature explosion":
                        metadata.preventArmatureExplosion = true;
                        break;
                    case "adjustment":
                        metadata.position = child.translation ?? Vector3.zero;
                        var scale = child.scale ?? Vector3.one;
                        metadata.scale = scale;
                        if (scale.x != scale.y || scale.y != scale.z) {
                            Plugin.logger.LogWarning($"Pretender {ouji.name} has a non-uniform scale adjustment. Import results may be dubious.");
                        }
                        if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0) {
                            Plugin.logger.LogWarning($"Pretender {ouji.name} has a non-positive scale adjustment. Import results may be dubious.");
                        }
                        break;
                    case "mipmap":
                        metadata.mipmap = true;
                        break;
                }
            }
        }

        foreach (var node in newBones) {
            var bone = new GameObject(node.name);
            // This hideFlags is load-bearing for e.g. Dega's tail
            bone.hideFlags = HideFlags.HideAndDontSave;
            bones.Add(bone.name, bone.transform);
        }
        foreach (var node in newBones) {
            var bone = bones[node.name!];
            bone.SetParent(bones[parents[node].name!]);
            bone.localPosition = node.translation ?? Vector3.zero;
            bone.localRotation = node.rotation ?? Quaternion.identity;
            bone.localScale = node.scale ?? Vector3.one;
        }

        var uMaterials = new List<Material>();
        foreach (var gMat in file.materials) {
            var uMat = UnityObject.Instantiate(body_m.material);
            uMat.name = gMat.name;
            if (Gltf.Loader.LoadTexture(file, gMat, binary, metadata.mipmap) is Texture2D uTex) {
                uMat.mainTexture = uTex;
            }
            uMaterials.Add(uMat);
        }

        foreach (var node in meshNodes) {
            var gMesh = file.meshes[node.mesh!.Value];
            if (gMesh.name == null) continue;

            var bodyPart = new GameObject(gMesh.name);
            bodyPart.transform.SetParent(body_root);
            bodyPart.hideFlags = HideFlags.HideAndDontSave;

            var renderer = bodyPart.AddComponent<SkinnedMeshRenderer>();
            var mesh = Gltf.Loader.LoadMesh(file, gMesh, binary, skinned: true);
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = gMesh.primitives
                .Select(p => p.material ?? 0)
                .Select(i => uMaterials[(int)i])
                .ToArray();

            if (node.skin is uint iSkin) {
                var skin = file.skins[iSkin];
                renderer.rootBone = bones["JNT_root"];

                renderer.bones = skin.joints.Select(iJoint => {
                    var joint = file.nodes[iJoint];
                    if (joint.name == null) throw new Exception($"Skin {skin.name} references nameless bone #{iJoint}");

                    if (bones.TryGetValue(joint.name, out var bone)) {
                        return bone;
                    } else {
                        return bones["JNT_root"];
                    }
                }).ToArray();

                if (skin.inverseBindMatrices is uint iBinds) {
                    var accessor = file.accessors[iBinds];
                    var bindposes = accessor.CopyOut<Matrix4x4>(file, binary);
                    for (var i = 0; i < bindposes.Length; i++) {
                        //bindposes[i] = bindposes[i].transpose;
                    }
                    mesh.bindposes = bindposes;
                } else {
                    Plugin.logger.LogWarning($"Skin {skin.name} has no inverse bind matrices.");
                    mesh.bindposes = skin.joints.Select(_ => Matrix4x4.identity).ToArray();
                }
            }

            // TODO: bindposes
            // TODO: face parts
        }

        foreach (var part in bodyParts.Values) {
            part.enabled = false;
        }
        bones["JNT_antenna"].GetChild(0).gameObject.SetActive(false);

        if (metadata.position != Vector3.zero || metadata.scale != Vector3.one) {
            var root = bones["JNT_root"];
            root.transform.localPosition = Vector3.Scale(metadata.position, metadata.scale);
            root.transform.localScale = metadata.scale;
        }

        if (metadata.preventArmatureExplosion) {
            ouji.AddComponent<PreventArmatureExplosion>();
        }
    }

    public static void ApplyBallModel(GameObject ball, Assimp.Scene scene) {
        if (scene.MeshCount > 1) {
            ApplyMultiBallModel(ball, scene);
            return;
        }

        var filter = ball.GetComponent<MeshFilter>();
        var renderer = ball.GetComponent<MeshRenderer>();

        var aMat = scene.Materials[0];
        var uTex = LoadTexture(scene, aMat, mipmap: true);
        var uMaterial = UnityObject.Instantiate(renderer.material);
        uMaterial.name = uTex.name = aMat.Name;
        uMaterial.mainTexture = uTex;

        var aMesh = scene.Meshes[0];
        var uMesh = new Mesh() { name = aMesh.Name };

        LoadVertices(aMesh, uMesh);
        LoadIndices(aMesh, uMesh);
        LoadUVs(aMesh, uMesh);
        LoadNormals(aMesh, uMesh);

        uMesh.UploadMeshData(markNoLongerReadable: true);

        filter.sharedMesh = uMesh;
        renderer.sharedMaterial = uMaterial;
    }

    private static void ApplyMultiBallModel(GameObject ball, Assimp.Scene scene) {
        var filter = ball.GetComponent<MeshFilter>();
        var renderer = ball.GetComponent<MeshRenderer>();

        var allMaterials = new List<Material>();
        foreach (var aMat in scene.Materials) {
            var uTex = LoadTexture(scene, aMat, mipmap: true);
            var uMaterial = UnityObject.Instantiate(renderer.material);
            uMaterial.name = uTex.name = aMat.Name;
            uMaterial.mainTexture = uTex;
            allMaterials.Add(uMaterial);
        }

        var vertices = new List<Vector3>();
        var indices = new List<int[]>();
        var topologies = new List<MeshTopology>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var materials = new List<Material>();

        foreach (var aMesh in scene.Meshes) {
            vertices.AddRange(aMesh.Vertices.Select(v => v.ToUnity()));
            indices.Add(aMesh.Faces.SelectMany(f => f.Indices).Select(i => i).ToArray());
            uvs.AddRange(aMesh.TextureCoordinateChannels[0].Select(v => v.ToUnityVec2()));
            normals.AddRange(aMesh.Normals.Select(v => v.ToUnity()));
            materials.Add(allMaterials[aMesh.MaterialIndex]);

            topologies.Add(aMesh.Faces[0].IndexCount switch {
                3 => MeshTopology.Triangles,
                4 => MeshTopology.Quads,
                _ => throw new InvalidOperationException(),
            });
        }

        var uMesh = new Mesh() { name = scene.Meshes[0].Name, subMeshCount = indices.Count };
        uMesh.SetVertices(vertices);

        var baseVertex = 0;
        for (var i = 0; i < indices.Count; i++) {
            uMesh.SetIndices(indices[i], topologies[i], submesh: i, calculateBounds: true, baseVertex);
            baseVertex += scene.Meshes[i].VertexCount;
        }

        uMesh.SetUVs(0, uvs);
        uMesh.SetNormals(normals);

        uMesh.UploadMeshData(markNoLongerReadable: true);

        filter.sharedMesh = uMesh;
        renderer.sharedMaterials = materials.ToArray();
    }

    private static Texture2D LoadTexture(Assimp.Scene scene, Assimp.Material aMat, bool mipmap) {
        var path = aMat.TextureDiffuse.FilePath;

        byte[] data;

        if (path.StartsWith("*")) {
            var i = int.Parse(path.Substring(1));
            data = scene.Textures[i].CompressedData;
        } else if (path == null) {
            throw new Exception($"FBX material {aMat.Name} has no texture");
        } else {
            data = File.ReadAllBytes(path);
        }

        var uTex = new Texture2D(0, 0, TextureFormat.RGBA32, mipmap);
        ImageConversion.LoadImage(uTex, data, markNonReadable: true);

        if (mipmap) {
            uTex.anisoLevel = 16;
            uTex.filterMode = FilterMode.Trilinear;
        } else {
            uTex.filterMode = FilterMode.Point;
        }
        return uTex;
    }

    private static void LoadVertices(Assimp.Mesh aMesh, Mesh uMesh) {
        var vertices = aMesh.Vertices.Select(v => v.ToUnity()).ToList();
        uMesh.SetVertices(vertices);
    }

    private static void LoadIndices(Assimp.Mesh aMesh, Mesh uMesh) {
        var topo = aMesh.Faces[0].IndexCount switch {
            3 => MeshTopology.Triangles,
            4 => MeshTopology.Quads,
            _ => throw new InvalidOperationException(),
        };
        var indices = new List<int>(3 * aMesh.FaceCount);
        foreach (var face in aMesh.Faces) indices.AddRange(face.Indices);
        //indices.Reverse(); // I still need to figure out exactly what's going on but this is sensitive to winding order.
        uMesh.SetIndices(indices.ToArray(), topo, 0);
    }

    private static void LoadUVs(Assimp.Mesh aMesh, Mesh uMesh) {
        var uvs = aMesh.TextureCoordinateChannels[0].Select(v => v.ToUnityVec2()).ToList();
        uMesh.SetUVs(0, uvs);
    }

    private static void LoadNormals(Assimp.Mesh aMesh, Mesh uMesh) {
        if (aMesh.HasNormals) {
            var normals = aMesh.Normals.Select(n => n.ToUnity()).ToList();
            uMesh.SetNormals(normals);
        } else {
            uMesh.RecalculateNormals();
        }
    }

    private static void PrintNode(Assimp.Scene scene, Assimp.Node node, int depth = 0) {
        var indent = "";
        for (var i = 0; i < depth; i++) indent += "  ";

        Console.WriteLine(indent + node.Name);
        node.Transform.Decompose(out var size, out var rot, out var pos);
        Console.WriteLine(indent + rot);
        Console.WriteLine(indent + pos);
        Console.WriteLine(indent + size);

        foreach (var meshIdx in node.MeshIndices) {
            Console.WriteLine(indent + "  * " + scene.Meshes[meshIdx].Name);
        }
        foreach (var child in node.Children) {
            PrintNode(scene, child, depth + 1);
        }
    }
}