using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCrushers;

public static class GameObjectUtility
{
	public static T FindFirstObjectByType<T>() where T : UnityEngine.Object
	{
		return UnityEngine.Object.FindObjectOfType<T>();
	}

	public static UnityEngine.Object FindFirstObjectByType(Type type)
	{
		return UnityEngine.Object.FindObjectOfType(type);
	}

	public static T[] FindObjectsByType<T>() where T : UnityEngine.Object
	{
		return UnityEngine.Object.FindObjectsOfType<T>();
	}

	public static UnityEngine.Object[] FindObjectsByType(Type type)
	{
		return UnityEngine.Object.FindObjectsOfType(type);
	}

	public static bool IsPrefab(GameObject go)
	{
		if (go == null)
		{
			return false;
		}
		if (go.activeInHierarchy)
		{
			return false;
		}
		if (go.transform.parent != null && go.transform.parent.gameObject.activeSelf)
		{
			return false;
		}
		GameObject[] array = FindObjectsByType<GameObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == go)
			{
				return false;
			}
		}
		return true;
	}

	public static bool DoesGameObjectPathMatch(Transform t, string requiredPath)
	{
		if (t == null)
		{
			return false;
		}
		if (t.name.Contains("/"))
		{
			return GetHierarchyPath(t).EndsWith(requiredPath);
		}
		return string.Equals(t.name, requiredPath);
	}

	public static string GetHierarchyPath(Transform t)
	{
		if (t == null)
		{
			return string.Empty;
		}
		if (t.parent == null)
		{
			return t.name;
		}
		return GetHierarchyPath(t.parent) + "/" + t.name;
	}

	public static GameObject GameObjectHardFind(string goName, bool checkAllScenes = true)
	{
		if (string.IsNullOrEmpty(goName))
		{
			return null;
		}
		GameObject gameObject = GameObject.Find(goName);
		if (gameObject != null)
		{
			return gameObject;
		}
		if (checkAllScenes)
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				GameObject[] rootGameObjects = SceneManager.GetSceneAt(i).GetRootGameObjects();
				gameObject = GameObjectHardFindRootObjects(goName, string.Empty, rootGameObjects);
				if (gameObject != null)
				{
					return gameObject;
				}
			}
			return null;
		}
		GameObject[] rootGameObjects2 = SceneManager.GetActiveScene().GetRootGameObjects();
		return GameObjectHardFindRootObjects(goName, string.Empty, rootGameObjects2);
	}

	public static GameObject GameObjectHardFind(string goName, string tag, bool checkAllScenes = true)
	{
		GameObject gameObject = null;
		GameObject[] array = GameObject.FindGameObjectsWithTag(tag);
		foreach (GameObject gameObject2 in array)
		{
			if (string.Equals(gameObject2.name, goName))
			{
				return gameObject2;
			}
		}
		if (checkAllScenes)
		{
			List<GameObject> list = new List<GameObject>();
			Transform[] array2 = Resources.FindObjectsOfTypeAll<Transform>();
			foreach (Transform transform in array2)
			{
				if (transform.root.hideFlags == HideFlags.None)
				{
					list.Add(transform.gameObject);
				}
			}
			gameObject = GameObjectHardFindRootObjects(goName, tag, list.ToArray());
			if (gameObject != null)
			{
				return gameObject;
			}
			return null;
		}
		GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
		return GameObjectHardFindRootObjects(goName, tag, rootGameObjects);
	}

	private static GameObject GameObjectHardFindRootObjects(string goName, string tag, GameObject[] rootGameObjects)
	{
		if (rootGameObjects == null)
		{
			return null;
		}
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			GameObject gameObject = GameObjectSearchHierarchy(rootGameObjects[i].transform, goName, tag);
			if (gameObject != null)
			{
				return gameObject;
			}
		}
		return null;
	}

	private static GameObject GameObjectSearchHierarchy(Transform t, string goName, string tag)
	{
		if (t == null)
		{
			return null;
		}
		if (string.Equals(t.name, goName) && (string.IsNullOrEmpty(tag) || string.Equals(t.tag, tag)))
		{
			return t.gameObject;
		}
		foreach (Transform item in t)
		{
			GameObject gameObject = GameObjectSearchHierarchy(item, goName, tag);
			if (gameObject != null)
			{
				return gameObject;
			}
		}
		return null;
	}

	public static T[] FindObjectsOfTypeAlsoInactive<T>(bool checkAllScenes = true) where T : Component
	{
		List<T> list = new List<T>();
		T[] array = Resources.FindObjectsOfTypeAll<T>();
		foreach (T val in array)
		{
			if (val.transform.root.hideFlags == HideFlags.None)
			{
				list.Add(val);
			}
		}
		return list.ToArray();
	}

	private static void FindObjectsSearchRootObjects<T>(GameObject[] rootGameObjects, List<T> list) where T : Component
	{
		if (rootGameObjects != null)
		{
			for (int i = 0; i < rootGameObjects.Length; i++)
			{
				FindObjectsSearchHierarchy(rootGameObjects[i].transform, list);
			}
		}
	}

	private static void FindObjectsSearchHierarchy<T>(Transform t, List<T> list) where T : Component
	{
		if (t == null)
		{
			return;
		}
		T[] components = t.GetComponents<T>();
		if (components.Length != 0)
		{
			list.AddRange(components);
		}
		foreach (Transform item in t)
		{
			FindObjectsSearchHierarchy(item, list);
		}
	}

	public static T GetComponentAnywhere<T>(GameObject gameObject) where T : Component
	{
		if (!gameObject)
		{
			return null;
		}
		T componentInChildren = gameObject.GetComponentInChildren<T>();
		if ((bool)componentInChildren)
		{
			return componentInChildren;
		}
		Transform parent = gameObject.transform.parent;
		int num = 0;
		while (!componentInChildren && (bool)parent && num < 256)
		{
			componentInChildren = parent.GetComponentInChildren<T>();
			parent = parent.parent;
		}
		return componentInChildren;
	}

	public static float GetGameObjectHeight(GameObject gameObject)
	{
		CharacterController component = gameObject.GetComponent<CharacterController>();
		if (component != null)
		{
			return component.height;
		}
		CapsuleCollider component2 = gameObject.GetComponent<CapsuleCollider>();
		if (component2 != null)
		{
			return component2.height;
		}
		BoxCollider component3 = gameObject.GetComponent<BoxCollider>();
		if (component3 != null)
		{
			return component3.center.y + component3.size.y;
		}
		SphereCollider component4 = gameObject.GetComponent<SphereCollider>();
		if (component4 != null)
		{
			return component4.center.y + component4.radius;
		}
		return 0f;
	}
}
