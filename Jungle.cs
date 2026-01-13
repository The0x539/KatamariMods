using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace KatamariDama60;

public static class Jungle {
    private static class Prefabs {
        public static GameObject billboard = null!;
        public static Material material = null!;
    }

    public static IEnumerator Init() {
        const string sceneName = "UI_MainMenu";

        var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        //load.allowSceneActivation = false;
        yield return load;

        var scene = SceneManager.GetSceneByName(sceneName);

        KinokoRotator? kinoko = null;
        foreach (var obj in scene.GetRootGameObjects()) {
            if (obj.name == "go_kinokoRotator") {
                kinoko = obj.GetComponent<KinokoRotator>();
            }
        }
        if (kinoko is null) yield break;

        Prefabs.billboard = Object.Instantiate(kinoko.jungleBoard);
        Prefabs.billboard.name = "JungleBillboardPrefab";
        Prefabs.material = Object.Instantiate(kinoko.matJungle);
        Prefabs.material.name = "JungleMaterialPrefab";
        Object.DontDestroyOnLoad(Prefabs.billboard);
        Object.DontDestroyOnLoad(Prefabs.material);

        yield return SceneManager.UnloadSceneAsync(sceneName);
    }

    public static void Dress(GameObject ouji) {
        var sceneName = ouji.scene.name;
        Console.WriteLine($"Trying to dress {ouji.name}, child of {ouji.transform.root.gameObject.name}, in scene {sceneName}");

        if (Prefabs.billboard == null) {
            Console.WriteLine("Oh no, billboard is null");
            return;
        } else if (Prefabs.material == null) {
            Console.WriteLine("Oh no, material is null");
            return;
        }

        var billboard = Object.Instantiate(Prefabs.billboard);
        var material = new Material(Prefabs.material);

        billboard.name = "JungleBoardEnding";
        billboard.transform.SetParent(ouji.transform, worldPositionStays: false);
        billboard.layer = ouji.layer;
        billboard.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");

        var hackPending = new List<GameObject>();

        foreach (var renderer in ouji.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            if (renderer?.name is "head_tawara_m" or "body01_m" or "hand_m") {
                if (sceneName == "Result2") {
                    hackPending.Add(renderer.gameObject);
                } else {
                    renderer.material = material;
                }
            }
        }

        billboard.transform.localScale = Vector3.one * 5;
        billboard.AddComponent<FaceCamera>();

        if (sceneName == "Result2") {
            foreach (var obj in hackPending) {
                var copy = Object.Instantiate(obj);
                copy.name = obj.name;
                obj.name += " (Jungle Depth Buffer Hack)";
                copy.transform.parent = obj.transform.parent;
                copy.GetComponent<SkinnedMeshRenderer>().material = material;
            }

            billboard.transform.localScale = Vector3.one * 2.5f;
            billboard.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        }
    }

    public sealed class FaceCamera : MonoBehaviour {
        public void Update() {
            if (Camera.main != null) {
                this.transform.LookAt(Camera.main.transform);
            }
        }
    }
}
