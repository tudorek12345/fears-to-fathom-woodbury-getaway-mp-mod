using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    public static class NetPath
    {
        private static readonly Dictionary<string, Transform> PathCache = new Dictionary<string, Transform>();

        public static string GetPath(Transform transform)
        {
            if (transform == null) return string.Empty;

            var sb = new StringBuilder();
            var current = transform;
            while (current != null)
            {
                sb.Insert(0, "/" + current.name + "[" + current.GetSiblingIndex() + "]");
                current = current.parent;
            }

            return transform.gameObject.scene.name + sb.ToString();
        }

        public static Transform FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (PathCache.TryGetValue(path, out var cached))
            {
                if (cached == null)
                {
                    PathCache.Remove(path);
                }
                else
                {
                    var cachedScene = cached.gameObject.scene;
                    if (cachedScene.IsValid() && cachedScene.isLoaded)
                    {
                        return cached;
                    }

                    PathCache.Remove(path);
                }
            }

            var slashIndex = path.IndexOf('/');
            if (slashIndex <= 0) return null;

            var sceneName = path.Substring(0, slashIndex);
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded) return null;

            var rootSegment = path.Substring(slashIndex + 1);
            var segments = rootSegment.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return null;

            Transform current = null;
            var roots = scene.GetRootGameObjects();
            current = FindChild(roots, segments[0]);
            if (current == null)
            {
                current = FindByLoosePath(scene, segments);
                if (current == null)
                {
                    current = FindUniqueByName(scene, ParseSegment(segments[segments.Length - 1]).name);
                }
                if (current != null)
                {
                    PathCache[path] = current;
                }
                return current;
            }

            for (var i = 1; i < segments.Length; i++)
            {
                current = FindChild(current, segments[i]);
                if (current == null)
                {
                    current = FindByLoosePath(scene, segments);
                    if (current == null)
                    {
                        current = FindUniqueByName(scene, ParseSegment(segments[segments.Length - 1]).name);
                    }
                    break;
                }
            }

            if (current != null)
            {
                PathCache[path] = current;
            }

            return current;
        }

        private static Transform FindChild(GameObject[] roots, string segment)
        {
            var (name, index) = ParseSegment(segment);
            if (index >= 0 && index < roots.Length)
            {
                var candidate = roots[index];
                if (candidate.name == name)
                {
                    return candidate.transform;
                }
            }

            var matches = new List<Transform>();
            foreach (var root in roots)
            {
                if (root.name == name)
                {
                    matches.Add(root.transform);
                }
            }

            if (index >= 0 && index < matches.Count)
            {
                return matches[index];
            }

            return matches.Count > 0 ? matches[0] : null;
        }

        private static Transform FindChild(Transform parent, string segment)
        {
            var (name, index) = ParseSegment(segment);
            if (index >= 0 && index < parent.childCount)
            {
                var candidate = parent.GetChild(index);
                if (candidate.name == name)
                {
                    return candidate;
                }
            }

            var matches = new List<Transform>();
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                {
                    matches.Add(child);
                }
            }

            if (index >= 0 && index < matches.Count)
            {
                return matches[index];
            }

            return matches.Count > 0 ? matches[0] : null;
        }

        private static (string name, int index) ParseSegment(string segment)
        {
            var start = segment.LastIndexOf('[');
            var end = segment.LastIndexOf(']');
            if (start > 0 && end > start)
            {
                var name = segment.Substring(0, start);
                if (int.TryParse(segment.Substring(start + 1, end - start - 1), out var index))
                {
                    return (name, index);
                }
            }

            return (segment, -1);
        }

        private static Transform FindByLoosePath(Scene scene, string[] segments)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            if (segments == null || segments.Length == 0) return null;

            var names = new string[segments.Length];
            for (var i = 0; i < segments.Length; i++)
            {
                names[i] = ParseSegment(segments[i]).name;
            }

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var match = FindByNamePath(root.transform, names, 0);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform FindByNamePath(Transform current, string[] names, int depth)
        {
            if (current == null) return null;
            if (current.name != names[depth]) return null;
            if (depth == names.Length - 1) return current;

            for (var i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                var match = FindByNamePath(child, names, depth + 1);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform FindUniqueByName(Scene scene, string targetName)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            if (string.IsNullOrEmpty(targetName)) return null;

            Transform match = null;
            var stack = new Stack<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                stack.Push(root.transform);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null) continue;
                if (current.name == targetName)
                {
                    if (match != null)
                    {
                        return null;
                    }
                    match = current;
                }
                for (var i = 0; i < current.childCount; i++)
                {
                    stack.Push(current.GetChild(i));
                }
            }

            return match;
        }
    }
}
