using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    public static class NetPath
    {
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
            if (current == null) return null;

            for (var i = 1; i < segments.Length; i++)
            {
                current = FindChild(current, segments[i]);
                if (current == null) return null;
            }

            return current;
        }

        private static Transform FindChild(GameObject[] roots, string segment)
        {
            var (name, index) = ParseSegment(segment);
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

            return null;
        }

        private static Transform FindChild(Transform parent, string segment)
        {
            var (name, index) = ParseSegment(segment);
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

            return null;
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

            return (segment, 0);
        }
    }
}
