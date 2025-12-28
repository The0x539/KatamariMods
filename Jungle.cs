using System;
using System.Collections;
using System.Linq;

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

        var kinoko = scene.GetRootGameObjects()
            .First(obj => obj.name == "go_kinokoRotator")
            .GetComponent<KinokoRotator>();

        Prefabs.billboard = Object.Instantiate(kinoko.jungleBoard);
        Prefabs.billboard.name = "JungleBillboardPrefab";
        Prefabs.material = Object.Instantiate(kinoko.matJungle);
        Prefabs.material.name = "JungleMaterialPrefab";
        Object.DontDestroyOnLoad(Prefabs.billboard);
        Object.DontDestroyOnLoad(Prefabs.material);

        yield return SceneManager.UnloadSceneAsync(sceneName);
    }

    public static void Dress(GameObject ouji) {
        Console.WriteLine($"Trying to dress {ouji.name}, child of {ouji.transform.root.gameObject.name}, in scene {ouji.scene.name}");

        if (Prefabs.billboard == null) {
            Console.WriteLine("Oh no, billboard is null");
            return;
        } else if (Prefabs.material == null) {
            Console.WriteLine("Oh no, material is null");
            return;
        }

        var billboard = Object.Instantiate(Prefabs.billboard);
        //var material = new Material(Prefabs.material);

        billboard.name = "JungleBoardEnding";
        billboard.transform.SetParent(ouji.transform, worldPositionStays: false);
        billboard.layer = ouji.layer;
        billboard.transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");

        foreach (var renderer in ouji.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            if (renderer?.name is "head_tawara_m" or "body01_m" or "hand_m") {
                renderer.material = new Material(Prefabs.material);
            }
        }

        billboard.transform.localScale = Vector3.one * 5;
        billboard.AddComponent<FaceCamera>();
    }

    public sealed class FaceCamera : MonoBehaviour {
        private Camera? camera = null;

        private Camera? DetermineCamera() {
            var sceneName = this.gameObject.scene.name;
            var rootName = this.gameObject.transform.root.name;

            if (rootName == "GO_ouji_motion") return Camera.main;

            return sceneName switch {
                "Title3" => Camera.main,
                "UI_MainMenu" or "UI_OujiStar" => Camera.main,
                "UI_Star" => FindObjectOfType<UIMonoCamera>()?._camera,
                //"UI_MainMenu" or "UI_OujiStar" => FindObjectOfType<UIMonoCamera>()?._camera,
                //"UI_OujiStar" => Resources.FindObjectsOfTypeAll<KinokoRotator>()[0]._camera,
                //"UI_OujiStar" => Resources.FindObjectsOfTypeAll<StartsMover>()[0]._cameraMain,
                _ => Camera.main,
            };
        }

        public void OnEnable() {
            var sceneName = this.gameObject.scene.name;
            var rootName = this.gameObject.transform.root.name;

            this.camera = this.DetermineCamera();
            var name = this.camera?.gameObject.name;
            if (name == null) name = "null";
            if (name == "") name = "no-name";
            Console.WriteLine($"Camera for {sceneName}::{rootName}: {name}");
        }

        public void Update() {
            if (this.camera != null) {
                this.transform.LookAt(this.camera.transform);
            }
        }
    }
}
