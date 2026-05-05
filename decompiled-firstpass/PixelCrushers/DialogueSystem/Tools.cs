using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCrushers.DialogueSystem;

public static class Tools
{
	public static Regex TextMeshProTagsRegex = new Regex("<[Bb]>|</[Bb]>|<[Ii]>|</[Ii]>|<color=[#]?\\w+>|<color=\"\\w+\">|</color>|<#\\w+>|<align=\\w+>|</align>|<font=[^>]+>|</font>|<indent=\\w+\\%>|<indent=\\w+>|</indent>|<line-height=\\w+%>|<line-height=\\w+>|</line-height>|<line-indent=\\w+\\%>|<line-ident=\\w+>|</line-ident>|<link=\"[^\"]+\">|</link>|<lowercase>|</lowercase>|<uppercase>|</uppercase>|<smallcaps>|</smallcaps>|<margin=.+?>|<margin-?\\w+=.+?>|</margin>|<mark=#\\w+>|</mark>|<nobr>|</nobr>|<size=\\w+\\%>|<size=\\w+>|</size>|<sprite=.+?>|<[Ss]>|</[Ss]>|<[Uu]>|</[Uu]>|<sup>|</sup>|<sub>|</sub>|<p>|</p>|<\\\\/p>");

	private static CursorLockMode previousLockMode = CursorLockMode.Locked;

	private static string[] htmlTags = new string[16]
	{
		"<html>", "<head>", "<style>", "#s0", "{text-align:left;}", "#s1", "{font-size:11pt;}", "</style>", "</head>", "<body>",
		"<p id=\"s0\">", "<span id=\"s1\">", "</span>", "</p>", "</body>", "</html>"
	};

	private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;

	private static readonly Regex StylesRegex = new Regex("<style>(?<styles>.*?)</style>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

	private static readonly Regex StyleRegex = new Regex("#(?<id>s[1-9]\\d*) {(?<style>.*?)}", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

	private static readonly Regex BoldRegex = new Regex("font-weight\\s*?:\\s*?bold", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

	private static readonly Regex ItalicRegex = new Regex("font-style\\s*?:\\s*?italic", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

	private static readonly Regex ColorRegex = new Regex("color\\s*?:\\s*?(?<color>#\\w{6})", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

	private static readonly Regex TextRegex = new Regex("<p id=\"s0\">(?<text>.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

	private static readonly Regex PartsRegex = new Regex("<span id=\"(?<id>s[1-9]\\d*)\">(?<text>.*?)</span>", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant);

	public static string loadedLevelName => SceneManager.GetActiveScene().name;

	public static void DeprecationWarning(MonoBehaviour mb, string extraInfo = null)
	{
		if (!(mb == null))
		{
			Debug.LogWarning("Dialogue System: " + mb.GetType().Name + " is deprecated and will be removed in the next version. " + extraInfo + "\nTo supress this message, add the scripting define symbol SUPPRESS_DEPRECATION_WARNINGS", mb);
		}
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
		GameObject[] array = GameObjectUtility.FindObjectsByType<GameObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] == go)
			{
				return false;
			}
		}
		return true;
	}

	public static byte HexToByte(string hex)
	{
		return byte.Parse(hex, NumberStyles.HexNumber);
	}

	public static bool IsNumber(object o)
	{
		if (!(o is int) && !(o is float))
		{
			return o is double;
		}
		return true;
	}

	public static int StringToInt(string s)
	{
		return SafeConvert.ToInt(s);
	}

	public static float StringToFloat(string s)
	{
		return SafeConvert.ToFloat(s);
	}

	public static bool StringToBool(string s)
	{
		return string.Compare(s, "True", StringComparison.OrdinalIgnoreCase) == 0;
	}

	public static bool IsStringNullOrEmptyOrWhitespace(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return string.IsNullOrEmpty(s.Trim());
		}
		return true;
	}

	public static string GetAllAfterSlashes(string s)
	{
		if (string.IsNullOrEmpty(s) || !s.Contains("/"))
		{
			return s;
		}
		int num = s.LastIndexOf("/") + 1;
		if (0 >= num || num >= s.Length)
		{
			return s;
		}
		return s.Substring(num);
	}

	public static string GetObjectName(UnityEngine.Object o)
	{
		if (!(o != null))
		{
			return "null";
		}
		return o.name;
	}

	public static string GetGameObjectName(Component c)
	{
		if (!(c == null))
		{
			return c.name;
		}
		return string.Empty;
	}

	public static string GetFullName(GameObject go)
	{
		string text = string.Empty;
		if (go != null)
		{
			text = go.name;
			Transform parent = go.transform.parent;
			while (parent != null)
			{
				text = parent.name + "." + text;
				parent = parent.parent;
			}
		}
		return text;
	}

	public static Transform Select(params Transform[] args)
	{
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] != null)
			{
				return args[i];
			}
		}
		return null;
	}

	public static MonoBehaviour Select(params MonoBehaviour[] args)
	{
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] != null)
			{
				return args[i];
			}
		}
		return null;
	}

	public static void SendMessageToEveryone(string message)
	{
		GameObject[] array = GameObjectUtility.FindObjectsByType<GameObject>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SendMessage(message, SendMessageOptions.DontRequireReceiver);
		}
	}

	public static void SendMessageToEveryone(string message, string arg)
	{
		GameObject[] array = GameObjectUtility.FindObjectsByType<GameObject>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SendMessage(message, arg, SendMessageOptions.DontRequireReceiver);
		}
	}

	public static IEnumerator SendMessageToEveryoneAsync(string message, int gameObjectsPerFrame)
	{
		GameObject[] gameObjects = GameObjectUtility.FindObjectsByType<GameObject>();
		int count = 0;
		for (int i = 0; i < gameObjects.Length; i++)
		{
			gameObjects[i].SendMessage(message, SendMessageOptions.DontRequireReceiver);
			count++;
			if (count >= gameObjectsPerFrame)
			{
				count = 0;
				yield return null;
			}
		}
	}

	public static void SetGameObjectActive(Component component, bool value)
	{
		if (component != null)
		{
			component.gameObject.SetActive(value);
		}
	}

	public static void SetGameObjectActive(GameObject gameObject, bool value)
	{
		if (gameObject != null)
		{
			gameObject.SetActive(value);
		}
	}

	public static bool ApproximatelyZero(float x)
	{
		return x < 0.0001f;
	}

	public static Color WebColor(string colorCode)
	{
		int r = ((colorCode.Length > 2) ? HexToByte(colorCode.Substring(1, 2)) : 0);
		byte g = (byte)((colorCode.Length > 4) ? HexToByte(colorCode.Substring(3, 2)) : 0);
		byte b = (byte)((colorCode.Length > 6) ? HexToByte(colorCode.Substring(5, 2)) : 0);
		return new Color32((byte)r, g, b, byte.MaxValue);
	}

	public static string ToWebColor(Color color)
	{
		return $"#{(int)(255f * color.r):x2}{(int)(255f * color.g):x2}{(int)(255f * color.b):x2}{(int)(255f * color.a):x2}";
	}

	public static string StripRichTextCodes(string s)
	{
		if (!s.Contains("<"))
		{
			return s;
		}
		return Regex.Replace(s, "<b>|</b>|<i>|</i>|<p>|</p>|<\\\\/p>|<color=[#]?\\w+>|</color>", string.Empty);
	}

	public static string StripTextMeshProTags(string s)
	{
		if (!s.Contains("<"))
		{
			return s;
		}
		return TextMeshProTagsRegex.Replace(s, string.Empty);
	}

	public static bool IsClipInAnimations(Animation animation, string clipName)
	{
		if (animation != null)
		{
			foreach (AnimationState item in animation)
			{
				if (string.Equals(item.name, clipName))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static GameObject GameObjectHardFind(string goName)
	{
		return GameObjectUtility.GameObjectHardFind(goName);
	}

	public static GameObject GameObjectHardFind(string goName, string tag)
	{
		return GameObjectUtility.GameObjectHardFind(goName, tag);
	}

	public static GameObject[] FindGameObjectsWithTagHard(string tag)
	{
		List<GameObject> list = new List<GameObject>();
		GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			GameObjectSearchForTags(rootGameObjects[i].transform, tag, list);
		}
		return list.ToArray();
	}

	private static void GameObjectSearchForTags(Transform t, string tag, List<GameObject> list)
	{
		if (t == null)
		{
			return;
		}
		if (string.Equals(t.tag, tag))
		{
			list.Add(t.gameObject);
		}
		foreach (Transform item in t)
		{
			GameObjectSearchForTags(item, tag, list);
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
		while (!componentInChildren && (bool)parent)
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

	public static void SetComponentEnabled(Component component, Toggle state)
	{
		if (component == null)
		{
			return;
		}
		bool flag;
		if (component is Renderer)
		{
			Renderer obj = component as Renderer;
			flag = (obj.enabled = ToggleUtility.GetNewValue(obj.enabled, state));
		}
		else if (component is Collider)
		{
			Collider obj2 = component as Collider;
			flag = (obj2.enabled = ToggleUtility.GetNewValue(obj2.enabled, state));
		}
		else if (component is Animation)
		{
			Animation obj3 = component as Animation;
			flag = (obj3.enabled = ToggleUtility.GetNewValue(obj3.enabled, state));
		}
		else if (component is Animator)
		{
			Animator obj4 = component as Animator;
			flag = (obj4.enabled = ToggleUtility.GetNewValue(obj4.enabled, state));
		}
		else if (component is AudioSource)
		{
			AudioSource obj5 = component as AudioSource;
			flag = (obj5.enabled = ToggleUtility.GetNewValue(obj5.enabled, state));
		}
		else
		{
			if (!(component is Behaviour))
			{
				if (DialogueDebug.logWarnings)
				{
					Debug.LogWarning(string.Format("{0}: Don't know how to enable/disable {1}.{2}", new object[3]
					{
						"Dialogue System",
						component.name,
						component.GetType().Name
					}));
				}
				return;
			}
			Behaviour obj6 = component as Behaviour;
			flag = (obj6.enabled = ToggleUtility.GetNewValue(obj6.enabled, state));
		}
		if (DialogueDebug.logInfo)
		{
			Debug.Log(string.Format("{0}: {1}.{2}.enabled = {3}", "Dialogue System", component.name, component.GetType().Name, flag));
		}
	}

	public static bool IsCursorActive()
	{
		if (IsCursorVisible())
		{
			return !IsCursorLocked();
		}
		return false;
	}

	public static void SetCursorActive(bool value)
	{
		ShowCursor(value);
		LockCursor(!value);
	}

	public static bool IsCursorVisible()
	{
		return Cursor.visible;
	}

	public static bool IsCursorLocked()
	{
		return Cursor.lockState != CursorLockMode.None;
	}

	public static void ShowCursor(bool value)
	{
		Cursor.visible = value;
	}

	public static void LockCursor(bool value)
	{
		if (!value && IsCursorLocked())
		{
			previousLockMode = Cursor.lockState;
		}
		Cursor.lockState = (value ? previousLockMode : CursorLockMode.None);
	}

	public static void LoadLevel(int index)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Loading level #" + index);
		}
		SceneManager.LoadScene(index);
	}

	public static void LoadLevel(string name)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Loading level " + name);
		}
		SceneManager.LoadScene(name);
	}

	public static AsyncOperation LoadLevelAsync(string name)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Asynchronously loading level " + name);
		}
		return SceneManager.LoadSceneAsync(name);
	}

	public static AsyncOperation LoadLevelAsync(int index)
	{
		if (DialogueDebug.logInfo)
		{
			Debug.Log("Dialogue System: Asynchronously loading level " + index);
		}
		return SceneManager.LoadSceneAsync(index);
	}

	public static string RemoveHtml(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			s = ReplaceMarkup(s);
			string[] array = htmlTags;
			foreach (string oldValue in array)
			{
				s = s.Replace(oldValue, string.Empty);
			}
			if (s.Contains("&#"))
			{
				s = ReplaceHtmlCharacterCodes(s);
			}
			s = s.Replace("&quot;", "\"");
			s = s.Replace("&amp;", "&");
			s = s.Replace("&lt;", "<");
			s = s.Replace("&gt;", ">");
			s = s.Replace("&nbsp;", " ");
			s = s.Trim();
		}
		return s;
	}

	public static string ReplaceHtmlCharacterCodes(string s)
	{
		return new Regex("&#[0-9]+;").Replace(s, (Match match) => (!int.TryParse(match.Value.Substring(2, match.Value.Length - 3), out var result)) ? match.Value : char.ConvertFromUtf32(result).ToString());
	}

	private static string ReplaceMarkup(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		return ConvertToRichText(s);
	}

	private static string ConvertToRichText(string s)
	{
		s = s.Replace("&#39;", "'").Replace("<strong>", "<b>").Replace("</strong>", "</b>")
			.Trim();
		if (!StylesRegex.IsMatch(s))
		{
			return s;
		}
		string value = StylesRegex.Match(s).Value;
		var source = from Match match in StyleRegex.Matches(value)
			select new
			{
				Id = match.Groups["id"].Value,
				Style = match.Groups["style"].Value
			};
		var styles = source.Select(style => new
		{
			Id = style.Id,
			Bold = BoldRegex.IsMatch(style.Style),
			Italic = ItalicRegex.IsMatch(style.Style),
			Color = ColorRegex.Match(style.Style).Groups["color"].Value
		});
		MatchCollection matchCollection = TextRegex.Matches(s);
		List<string> list = new List<string>();
		foreach (object item in matchCollection)
		{
			string[] value2 = (from Match match in PartsRegex.Matches(item.ToString())
				select new
				{
					StyleId = match.Groups["id"].Value,
					Text = match.Groups["text"].Value
				}).Select(anon2 =>
			{
				var anon = styles.First(style => style.Id == anon2.StyleId);
				return ApplyStyle(anon2.Text, anon.Bold, anon.Italic, anon.Color);
			}).ToArray();
			string text = string.Join(string.Empty, value2);
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(text);
			}
		}
		return string.Join("\n", list.ToArray());
	}

	private static string ApplyStyle(string innerText, bool bold, bool italic, string color)
	{
		StringBuilder builder = new StringBuilder(innerText);
		if (bold)
		{
			WrapInTag(ref builder, "b");
		}
		if (italic)
		{
			WrapInTag(ref builder, "i");
		}
		if (color != string.Empty)
		{
			WrapInTag(ref builder, "color", color);
		}
		return builder.ToString();
	}

	private static void WrapInTag(ref StringBuilder builder, string tag, string value = "")
	{
		builder.Insert(0, (value != string.Empty) ? $"<{tag}={value}>" : $"<{tag}>");
		builder.Append($"</{tag}>");
	}
}
