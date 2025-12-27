using HarmonyLib;

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
        var load = SceneManager.LoadSceneAsync("UI_MainMenu", LoadSceneMode.Additive);
        //load.allowSceneActivation = false;
        yield return load;

        var scene = SceneManager.GetSceneByName("UI_MainMenu");

        foreach (var obj in scene.GetRootGameObjects()) {
            if (obj.name != "go_kinokoRotator") continue;
            var kinoko = obj.GetComponent<KinokoRotator>();
            Prefabs.billboard = kinoko.jungleBoard;
            Prefabs.material = kinoko.matJungle;
            break;
        }

        yield return SceneManager.UnloadSceneAsync(scene);
    }

    public static void Dress(GameObject ouji) {
        var billboard = Object.Instantiate(Prefabs.billboard);
        var material = new Material(Prefabs.material);

        billboard.name = "JungleBoardEnding";
        billboard.transform.SetParent(ouji.transform, worldPositionStays: false);
        billboard.transform.localScale = Vector3.one * 5;

        billboard.AddComponent<FaceCamera>();

        foreach (var renderer in ouji.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            if (renderer?.name is "head_tawara_m" or "body01_m" or "hand_m") {
                renderer.material = material;
            }
        }
    }

    public sealed class FaceCamera : MonoBehaviour {
        private Camera? camera = null;

        public void OnEnable() {
            var sceneName = this.gameObject.scene.name;
            this.camera = sceneName switch {
                "Title3" => Camera.main,
                "UI_MainMenu" => FindObjectOfType<UIMonoCamera>()?._camera,
                //"UI_OujiStar" => Resources.FindObjectsOfTypeAll<KinokoRotator>()[0]._camera,
                //"UI_OujiStar" => Resources.FindObjectsOfTypeAll<StartsMover>()[0]._cameraMain,
                _ => null,
            };
            var name = this.camera?.gameObject.name;
            if (name == null) name = "null";
            if (name == "") name = "no-name";
            Console.WriteLine($"Camera for {sceneName}: {name}");
        }

        public void Update() {
            if (this.camera != null) {
                this.transform.LookAt(this.camera.transform);
            }
        }
    }
}
