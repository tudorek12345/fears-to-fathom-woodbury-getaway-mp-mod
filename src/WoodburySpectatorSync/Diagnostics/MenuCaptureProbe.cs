using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using WoodburySpectatorSync.UI;

namespace WoodburySpectatorSync.Diagnostics
{
    // Opt-in graphics smoke capture, called only with WSS_CAPTURE_MENU set to a directory.
    internal sealed class MenuCaptureProbe : MonoBehaviour
    {
        internal void Begin(NativeCoopMenu menu, string directory, Action<string> log)
        { StartCoroutine(Capture(menu, directory, log)); }
        private IEnumerator Capture(NativeCoopMenu menu, string directory, Action<string> log)
        {
            Directory.CreateDirectory(directory);
            string prefix = Path.Combine(directory, System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            yield return new WaitForSecondsRealtime(3f);
            menu.SetOpen(false);
            Render(prefix + "-menu.png", menu, log);
            yield return new WaitForSecondsRealtime(1f);
            menu.SetOpen(true);
            yield return new WaitForSecondsRealtime(1f);
            Render(prefix + "-multiplayer.png", menu, log);
            yield return new WaitForSecondsRealtime(1f);
            menu.ShowCharacter();
            yield return new WaitForSecondsRealtime(1f);
            Render(prefix + "-character.png", menu, log);
            menu.PreviewFixture(1, false);
            yield return new WaitForSecondsRealtime(1f);
            Render(prefix + "-character-puffer.png", menu, log);
            menu.PreviewFixture(2, true);
            yield return new WaitForSecondsRealtime(1f);
            Render(prefix + "-character-face.png", menu, log);
            menu.SetOpen(false);
            log?.Invoke("MenuCapture complete prefix=" + prefix);
            Destroy(this);
        }

        private void Render(string path, NativeCoopMenu menu, Action<string> log)
        {
            var native = GetComponent<UIManagerMainMenu>();
            var originalCamera = native.mainCamera.GetComponent<Camera>();
            var canvas = native.mmButtonsPanel.GetComponentInParent<Canvas>();
            var root = new GameObject("Coop menu capture camera"); var camera = root.AddComponent<Camera>();
            camera.CopyFrom(originalCamera); camera.enabled = false;
            camera.cullingMask = -1; // Include the independent editor Canvas as well as the game's UI layer.
            camera.transform.SetPositionAndRotation(originalCamera.transform.position, originalCamera.transform.rotation);
            var target = new RenderTexture(1280,720,24); var pixels = new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previous = RenderTexture.active; var mode = canvas.renderMode; var worldCamera = canvas.worldCamera; float distance = canvas.planeDistance;
            var editor = menu.PanelCanvas;
            var editorMode = editor.renderMode; var editorCamera = editor.worldCamera; float editorDistance = editor.planeDistance;
            log?.Invoke("MenuCapture frame=" + Path.GetFileName(path) + " open=" + menu.IsOpen + " nativeButtons=" + native.mmButtonsPanel.gameObject.activeSelf + " editor=" + editor.gameObject.activeInHierarchy);
            try
            {
                camera.targetTexture = target; canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera; canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 1, 2);
                editor.renderMode = RenderMode.ScreenSpaceCamera; editor.worldCamera = camera; editor.planeDistance = canvas.planeDistance;
                Canvas.ForceUpdateCanvases(); menu.RefreshLayout(); Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
                var encode = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule").GetMethod("EncodeToPNG",
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Texture2D) }, null);
                File.WriteAllBytes(path,(byte[])encode.Invoke(null,new object[]{pixels}));
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = worldCamera; canvas.planeDistance = distance;
                editor.renderMode = editorMode; editor.worldCamera = editorCamera; editor.planeDistance = editorDistance;
                RenderTexture.active = previous; camera.targetTexture = null; target.Release();
                Destroy(target); Destroy(pixels); Destroy(root); Canvas.ForceUpdateCanvases();
            }
        }
    }
}
