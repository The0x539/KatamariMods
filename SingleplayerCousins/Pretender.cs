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
                Plugin.logger.LogWarning($"Unknown mission `{word.Trim()}`. Ignoring.");
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
        } else if (Plugin.OujiId == (int)PretenderId.Mint) {
            __result = id switch {
                "OT_OBJ_0504" => "Duawg",
                "OT_OBJ_0505" => "Duawg With Fleas",
                "OT_OBJ_0506" => "Bullduawg",
                "OT_OBJ_0527" => "Toy Duawg",
                _ => __result,
            };
            if (id.StartsWith("KG_C_VRG") || id.StartsWith("KG_O_SLC") || id.StartsWith("KG_O_RSL")) {
                __result = __result.Replace("maiden", "broad").Replace("Maiden", "Broad");
            }
        }
    }
}

internal static class PretenderLoader {
    public static void ApplyModel(GameObject ouji, string path) {
        Gltf.UnityJsonSerializerStrategy.Register();
        // TODO: cache the JSON and binary like with FBX
        var file = File.OpenRead(path);
        var reader = new BinaryReader(file);
        Gltf.Glb.Parse(reader, out var json, out var binary);
        var asset = Json.DeserializeObject<Gltf.AssetFile>(json)!;
        ApplyModel(ouji, asset, binary);
    }

    public static void ApplyBallModel(GameObject ball, string path) {
        Gltf.UnityJsonSerializerStrategy.Register();
        // TODO: cache the JSON and binary like with FBX
        var file = File.OpenRead(path);
        var reader = new BinaryReader(file);
        Gltf.Glb.Parse(reader, out var json, out var binary);
        var asset = Json.DeserializeObject<Gltf.AssetFile>(json)!;
        ApplyBallModel(ball, asset, binary);
    }

    private sealed class ImportMetadata {
        public bool preventArmatureExplosion = false;
        public Vector3 scale = Vector3.one;
        public Vector3 position = Vector3.zero;
        // Off by default because Soyo and Dega, the first two characters I was serious about adding,
        // both use pixel art textures, which Unity's lack of anisotropic point filtering.
        // As demonstrated with my work on terrain textures, this produces awful results,
        // so it's preferable to just disable mipmaps altogether for pixel art character textures.
        public bool mipmap = false;
        public bool alpha = false;
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

            if (node.name == "METADATA") {
                metadata = ScanMetadata(node, file);
            } else {
                foreach (var iChild in node.children) {
                    var child = file.nodes[iChild];
                    parents.Add(child, node);
                    nodes.Push(child);
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

        var uMaterials = LoadMaterials(file, body_m.sharedMaterial, binary, metadata);

        foreach (var node in meshNodes) {
            var gMesh = file.meshes[node.mesh!.Value];
            if (gMesh.name == null) continue;

            var bodyPart = new GameObject(gMesh.name);
            bodyPart.transform.SetParent(body_root);
            bodyPart.hideFlags = HideFlags.HideAndDontSave;

            var renderer = bodyPart.AddComponent<SkinnedMeshRenderer>();
            var mesh = Gltf.Loader.LoadMesh(file, gMesh, binary, skinned: true, morph: true);
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
                    mesh.bindposes = bindposes;
                } else {
                    Plugin.logger.LogWarning($"Skin {skin.name} has no inverse bind matrices.");
                    mesh.bindposes = skin.joints.Select(_ => Matrix4x4.identity).ToArray();
                }
            }

            foreach (var groupName in new[] { "eye", "face", "mouth", "parts" }) {
                if (!mesh.name.StartsWith(groupName + "[")) continue;

                var chop1 = mesh.name.Substring(groupName.Length + 1);
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

    public static void ApplyBallModel(GameObject ball, Gltf.AssetFile file, byte[] binary) {
        if (file.scene is not uint iScene) {
            throw new Exception("glTF file has no default scene!");
        }

        var filter = ball.GetComponent<MeshFilter>();
        var renderer = ball.GetComponent<MeshRenderer>();

        var nodes = new Stack<Gltf.Node>();
        foreach (var iRoot in file.scenes[iScene].nodes) {
            nodes.Push(file.nodes[iRoot]);
        }

        var meshNodes = new List<Gltf.Node>();
        var parents = new Dictionary<Gltf.Node, Gltf.Node>();
        var metadata = new ImportMetadata();

        while (nodes.Count > 0) {
            var node = nodes.Pop();
            if (node.name == null) continue;
            if (node.mesh != null) {
                meshNodes.Add(node);
            }
            if (node.name == "METADATA") {
                metadata = ScanMetadata(node, file);
            }
            // Ball models aren't expected to be complex enough to need child traversal.
        }

        var uMaterials = LoadMaterials(file, renderer.sharedMaterial, binary, metadata);

        // TODO: don't just assume it's a single gltf mesh lol? can we combine somehow
        var gMesh = file.meshes[meshNodes[0].mesh!.Value];

        var uMesh = Gltf.Loader.LoadMesh(file, gMesh, binary, skinned: false, morph: false, scale: 0.01f);
        var materials = gMesh.primitives
            .Select(p => p.material ?? 0)
            .Select(i => uMaterials[(int)i])
            .ToArray();

        filter.sharedMesh = uMesh;
        renderer.sharedMaterials = materials;
    }

    private static ImportMetadata ScanMetadata(Gltf.Node node, Gltf.AssetFile file) {
        var metadata = new ImportMetadata();
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
                        Plugin.logger.LogWarning("Custom model has a non-uniform scale adjustment. Import results may be dubious.");
                    }
                    if (scale.x <= 0 || scale.y <= 0 || scale.z <= 0) {
                        Plugin.logger.LogWarning("Custom model has a non-positive scale adjustment. Import results may be dubious.");
                    }
                    break;
                case "mipmap": metadata.mipmap = true; break;
                case "alpha": metadata.alpha = true; break;
            }
        }
        return metadata;
    }

    private static List<Material> LoadMaterials(Gltf.AssetFile file, Material origMat, byte[] binary, ImportMetadata metadata) {
        var uMaterials = new List<Material>();
        foreach (var gMat in file.materials) {
            var uMat = UnityObject.Instantiate(origMat);
            uMat.name = gMat.name;
            if (Gltf.Loader.LoadTexture(file, gMat, binary, metadata.mipmap) is Texture2D uTex) {
                uMat.mainTexture = uTex;
            }
            if (metadata.alpha) {
                uMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                uMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                uMat.EnableKeyword("_ALPHATEST_ON");
                uMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            } else if (uMat.name?.Contains("transparent") ?? false) {
                uMat.EnableKeyword("_ALPHATEST_ON");
                uMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            uMaterials.Add(uMat);
        }
        return uMaterials;
    }
}