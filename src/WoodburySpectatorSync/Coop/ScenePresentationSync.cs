using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using TypingGameKit;
using WoodburySpectatorSync.Net;
using static WoodburySpectatorSync.Net.ScenePresentationFrame;

namespace WoodburySpectatorSync.Coop
{
    // Host-authored presentation for credits and the additive Office computer game.
    // Game logic/input remains on the host; client actors, UI and animation only mirror it.
    internal sealed class ScenePresentationSync
    {
        public const string UiKind = "ScenePresentation";
        public const string TypingScene = "Typing Shooter Final scene";
        private sealed class Target { public string Path; public Transform Transform; public string Last; }
        private int _handle = -1;
        private readonly List<Target> _targets = new List<Target>();
        private readonly Dictionary<string, Transform> _byPath = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string, InputSequence> _words = new Dictionary<string, InputSequence>(StringComparer.Ordinal);
        private static readonly Dictionary<Type, PropertyInfo> TextProperties = new Dictionary<Type, PropertyInfo>();
        private static readonly Dictionary<Type, PropertyInfo> ColorProperties = new Dictionary<Type, PropertyInfo>();
        private float _nextLog;
        private TypingShooterAudioManager _typingAudio;

        public static bool Supports(string scene) => scene == "CreditsScene" || scene == TypingScene;

        private void Bind(Scene scene)
        {
            if (_handle == scene.handle) return;
            _handle = scene.handle; _targets.Clear(); _byPath.Clear(); _words.Clear(); _nextLog = 0f;
            _typingAudio = scene.name == TypingScene ? UnityEngine.Object.FindObjectOfType<TypingShooterAudioManager>() : null;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                {
                    if (root.name.StartsWith("Coop", StringComparison.Ordinal)) continue;
                    // Credits contains a static cabin set. Only its presentation changes;
                    // serializing thousands of environment meshes overflows the bounded frame.
                    if (scene.name == "CreditsScene" && !(tr is RectTransform) &&
                        tr.GetComponent<AudioSource>() == null && tr.GetComponent<Camera>() == null &&
                        tr.GetComponent<Animator>() == null) continue;
                    // Animator owns skeleton bones; sending those separately fights animation evaluation.
                    var animator = tr.GetComponentInParent<Animator>();
                    if (animator != null && animator.transform != tr) continue;
                    if (tr.GetComponentInParent<OverlaySequence>() != null) continue;
                    string path = NetPath.GetPath(tr);
                    _targets.Add(new Target { Path = path, Transform = tr }); _byPath[path] = tr;
                }
        }

        public string Capture(bool force, Action<string> log)
        {
            var scene = SceneManager.GetActiveScene();
            if (!Supports(scene.name)) return null;
            Bind(scene);
            if (force)
            {
                Behaviour authority = scene.name == TypingScene
                    ? (Behaviour)UnityEngine.Object.FindObjectOfType<TypingShooterStatesManager>()
                    : UnityEngine.Object.FindObjectOfType<EndCreditsManager>();
                log?.Invoke("ScenePresentationAuthority role=host scene=" + scene.name + " enabled=" + (authority != null && authority.enabled));
            }
            var nodes = new List<Node>();
            foreach (var target in _targets)
            {
                var tr = target.Transform;
                var node = new Node { path = target.Path, active = tr != null && tr.gameObject.activeSelf };
                if (tr != null)
                {
                    node.position = tr.localPosition; node.rotation = tr.localRotation; node.scale = tr.localScale;
                    var anim = tr.GetComponent<Animator>();
                    if (anim != null && anim.runtimeAnimatorController != null && anim.isActiveAndEnabled)
                    {
                        var state = anim.GetCurrentAnimatorStateInfo(0); node.animation = state.fullPathHash; node.time = state.normalizedTime;
                    }
                    var texts = new List<TextValue>();
                    var tints = new List<TintValue>();
                    foreach (var component in tr.GetComponents<Component>())
                    {
                        if (component == null) continue;
                        var prop = TextProperty(component.GetType());
                        if (prop != null) texts.Add(new TextValue { type = component.GetType().FullName, text = (string)prop.GetValue(component, null) });
                        var color = ColorProperty(component.GetType());
                        if (color != null) tints.Add(new TintValue { type = component.GetType().FullName, color = (Color)color.GetValue(component, null) });
                    }
                    node.texts = texts.ToArray();
                    node.tints = tints.ToArray();
                    var audio = tr.GetComponent<AudioSource>();
                    if (audio != null) { node.hasAudio = true; node.playing = audio.isPlaying; node.audioTime = audio.time; node.volume = audio.volume; }
                    var camera = tr.GetComponent<Camera>();
                    if (camera != null) { node.hasCamera = true; node.cameraEnabled = camera.enabled; node.fieldOfView = camera.fieldOfView; }
                }
                var json = ScenePresentationFrame.Fingerprint(node);
                if (force || json != target.Last) nodes.Add(node);
                target.Last = json;
            }
            var words = new List<Word>();
            if (scene.name == TypingScene)
                foreach (var overlay in UnityEngine.Object.FindObjectsOfType<OverlaySequence>())
                    if (overlay.gameObject.scene == scene && overlay.Target != null && overlay.InputSequence != null)
                        words.Add(new Word { target = NetPath.GetPath(overlay.Target), text = overlay.InputSequence.Text, progress = overlay.InputSequence.ProgressIndex });
            var player = TypingGameKit.AlienTyping.Player.Instance;
            int music = 0;
            if (_typingAudio != null && _typingAudio.audioSource != null && _typingAudio.audioSource.clip != null)
            {
                var clip = _typingAudio.audioSource.clip;
                music = clip == _typingAudio.menuStateClip ? 1 : clip == _typingAudio.playStateClip ? 2 : clip == _typingAudio.gameOverClip ? 3 : 0;
            }
            return new ScenePresentationFrame { nodes = nodes.ToArray(), words = words.ToArray(), health = player != null ? player.Health : 0, music = music }.Encode();
        }

        public bool Apply(string payload, Action<string> log)
        {
            var scene = SceneManager.GetActiveScene();
            if (!Supports(scene.name) || string.IsNullOrEmpty(payload)) return false;
            var frame = ScenePresentationFrame.Decode(payload);
            Bind(scene);
            Suppress(scene);
            if (_typingAudio != null && _typingAudio.audioSource != null)
            {
                var clip = frame.music == 1 ? _typingAudio.menuStateClip : frame.music == 2 ? _typingAudio.playStateClip : frame.music == 3 ? _typingAudio.gameOverClip : null;
                if (_typingAudio.audioSource.clip != clip) _typingAudio.audioSource.clip = clip;
            }
            int missing = 0;
            foreach (var node in frame.nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.path) || !node.path.StartsWith(scene.name + "/", StringComparison.Ordinal))
                {
                    log?.Invoke("ScenePresentation invalid path=" + node?.path + " scene=" + scene.name); return false;
                }
                if (!_byPath.TryGetValue(node.path, out var tr) || tr == null) { if (node.active) missing++; continue; }
                tr.gameObject.SetActive(node.active);
                if (!node.active) continue;
                tr.localPosition = node.position; tr.localRotation = node.rotation; tr.localScale = node.scale;
                var animator = tr.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null && node.animation != 0 && animator.HasState(0, node.animation))
                {
                    animator.fireEvents = false; animator.speed = 0f;
                    animator.Play(node.animation, 0, node.time); animator.Update(0f);
                }
                if (node.texts != null)
                    foreach (var text in node.texts)
                        foreach (var component in tr.GetComponents<Component>())
                            if (component != null && component.GetType().FullName == text.type)
                                TextProperty(component.GetType())?.SetValue(component, text.text ?? string.Empty, null);
                if (node.tints != null)
                    foreach (var tint in node.tints)
                        foreach (var component in tr.GetComponents<Component>())
                            if (component != null && component.GetType().FullName == tint.type)
                                ColorProperty(component.GetType())?.SetValue(component, tint.color, null);
                var audio = tr.GetComponent<AudioSource>();
                if (node.hasAudio && audio != null)
                {
                    audio.volume = Mathf.Clamp01(node.volume);
                    if (audio.clip != null && node.playing)
                    {
                        if (Mathf.Abs(audio.time - node.audioTime) > 0.3f) audio.time = Mathf.Clamp(node.audioTime, 0f, Mathf.Max(0f, audio.clip.length - 0.01f));
                        if (!audio.isPlaying) audio.Play();
                    }
                    else if (audio.isPlaying) audio.Stop();
                }
                var camera = tr.GetComponent<Camera>();
                if (node.hasCamera && camera != null) { camera.enabled = node.cameraEnabled; camera.fieldOfView = Mathf.Clamp(node.fieldOfView, 1f, 179f); }
            }
            var player = TypingGameKit.AlienTyping.Player.Instance;
            if (scene.name == TypingScene && player != null) player._health = Mathf.Clamp(frame.health, 0, 1000);
            missing += ApplyWords(scene, frame.words);
            // Reactivating a hierarchy can enable a native brain. Stop it before the next Update.
            Suppress(scene);
            if (Time.realtimeSinceStartup >= _nextLog)
            {
                _nextLog = Time.realtimeSinceStartup + 5f;
                log?.Invoke("ScenePresentation role=client scene=" + scene.name + " nodes=" + frame.nodes.Length + " words=" + frame.words.Length + " missing=" + missing);
            }
            return missing == 0;
        }

        private int ApplyWords(Scene scene, Word[] words)
        {
            if (scene.name != TypingScene) return 0;
            var manager = UnityEngine.Object.FindObjectOfType<OverlaySequenceManager>();
            if (manager == null) return words.Length;
            int missing = 0;
            var present = new HashSet<string>(StringComparer.Ordinal);
            foreach (var word in words)
            {
                if (word == null || string.IsNullOrEmpty(word.target) || !word.target.StartsWith(scene.name + "/", StringComparison.Ordinal) || word.text == null || word.text.Length > 2048) { missing++; continue; }
                present.Add(word.target);
                if (!_words.TryGetValue(word.target, out var sequence) || sequence == null)
                {
                    var target = NetPath.FindByPath(word.target);
                    if (target == null) { missing++; continue; }
                    sequence = manager.CreateSequence(word.text, target); _words[word.target] = sequence;
                }
                sequence.enabled = false;
                if (sequence.Text != word.text || sequence.ProgressIndex > word.progress) sequence.Text = word.text;
                // These presentation-only sequences have no Enemy completion callbacks.
                while (sequence.ProgressIndex < Mathf.Min(word.progress, word.text.Length))
                    if (!sequence.ProcessInput(word.text[sequence.ProgressIndex])) break;
            }
            foreach (var key in new List<string>(_words.Keys))
                if (!present.Contains(key)) { if (_words[key] != null) _words[key].Remove(); _words.Remove(key); }
            return missing;
        }

        public static void Suppress(Scene scene)
        {
            if (!Supports(scene.name)) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    var name = behaviour.GetType().FullName ?? "";
                    bool brain = behaviour is EndCreditsManager || behaviour is TypingShooterStatesManager ||
                        name.StartsWith("TypingGameKit.AlienTyping.", StringComparison.Ordinal) ||
                        behaviour is InputSequence || behaviour is TargetInputSequence || behaviour is InputSequenceCompleteOnClick ||
                        name == "UnityEngine.UI.Button" || name == "UnityEngine.UI.InputField" || name == "TMPro.TMP_InputField";
                    if (!brain) continue;
                    behaviour.StopAllCoroutines(); behaviour.CancelInvoke(); behaviour.enabled = false;
                }
                foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true)) agent.enabled = false;
                foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.fireEvents = false;
            }
        }

        private static PropertyInfo TextProperty(Type type)
        {
            if (TextProperties.TryGetValue(type, out var result)) return result;
            // Only actual UI text, never game input/state properties with side effects.
            var fullName = type.FullName ?? "";
            result = (fullName.StartsWith("TMPro.", StringComparison.Ordinal) || fullName == "UnityEngine.UI.Text") ? type.GetProperty("text") : null;
            if (result != null && (!result.CanRead || !result.CanWrite || result.PropertyType != typeof(string))) result = null;
            TextProperties[type] = result; return result;
        }

        private static PropertyInfo ColorProperty(Type type)
        {
            if (ColorProperties.TryGetValue(type, out var result)) return result;
            var name = type.FullName ?? "";
            result = name.StartsWith("UnityEngine.UI.", StringComparison.Ordinal) || name.StartsWith("TMPro.", StringComparison.Ordinal) ? type.GetProperty("color") : null;
            if (result != null && (!result.CanRead || !result.CanWrite || result.PropertyType != typeof(Color))) result = null;
            ColorProperties[type] = result; return result;
        }
    }
}
