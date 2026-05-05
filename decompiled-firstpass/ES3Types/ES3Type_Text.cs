using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[]
{
	"font", "text", "supportRichText", "resizeTextForBestFit", "resizeTextMinSize", "resizeTextMaxSize", "alignment", "alignByGeometry", "fontSize", "horizontalOverflow",
	"verticalOverflow", "lineSpacing", "fontStyle", "onCullStateChanged", "maskable", "color", "raycastTarget", "material", "useGUILayout", "enabled",
	"tag", "name", "hideFlags"
})]
public class ES3Type_Text : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3Type_Text()
		: base(typeof(Text))
	{
		Instance = this;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		Text val = (Text)obj;
		writer.WriteProperty("text", val.text);
		writer.WriteProperty("supportRichText", val.supportRichText);
		writer.WriteProperty("resizeTextForBestFit", val.resizeTextForBestFit);
		writer.WriteProperty("resizeTextMinSize", val.resizeTextMinSize);
		writer.WriteProperty("resizeTextMaxSize", val.resizeTextMaxSize);
		writer.WriteProperty("alignment", val.alignment);
		writer.WriteProperty("alignByGeometry", val.alignByGeometry);
		writer.WriteProperty("fontSize", val.fontSize);
		writer.WriteProperty("horizontalOverflow", val.horizontalOverflow);
		writer.WriteProperty("verticalOverflow", val.verticalOverflow);
		writer.WriteProperty("lineSpacing", val.lineSpacing);
		writer.WriteProperty("fontStyle", val.fontStyle);
		writer.WriteProperty("onCullStateChanged", ((MaskableGraphic)val).onCullStateChanged);
		writer.WriteProperty("maskable", ((MaskableGraphic)val).maskable);
		writer.WriteProperty("color", ((Graphic)val).color);
		writer.WriteProperty("raycastTarget", ((Graphic)val).raycastTarget);
		if (((Graphic)val).material.name.Contains("Default"))
		{
			writer.WriteProperty("material", null);
		}
		else
		{
			writer.WriteProperty("material", ((Graphic)val).material);
		}
		writer.WriteProperty("useGUILayout", ((MonoBehaviour)(object)val).useGUILayout);
		writer.WriteProperty("enabled", ((Behaviour)(object)val).enabled);
		writer.WriteProperty("hideFlags", ((Object)(object)val).hideFlags);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		Text val = (Text)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "m_FontData":
				reader.SetPrivateField("m_FontData", reader.Read<FontData>(), val);
				break;
			case "m_LastTrackedFont":
				reader.SetPrivateField("m_LastTrackedFont", reader.Read<Font>(), val);
				break;
			case "m_Text":
				reader.SetPrivateField("m_Text", reader.Read<string>(), val);
				break;
			case "m_TextCache":
				reader.SetPrivateField("m_TextCache", reader.Read<TextGenerator>(), val);
				break;
			case "m_TextCacheForLayout":
				reader.SetPrivateField("m_TextCacheForLayout", reader.Read<TextGenerator>(), val);
				break;
			case "m_Material":
				reader.SetPrivateField("m_Material", reader.Read<Material>(), val);
				break;
			case "font":
				val.font = reader.Read<Font>();
				break;
			case "text":
				val.text = reader.Read<string>();
				break;
			case "supportRichText":
				val.supportRichText = reader.Read<bool>();
				break;
			case "resizeTextForBestFit":
				val.resizeTextForBestFit = reader.Read<bool>();
				break;
			case "resizeTextMinSize":
				val.resizeTextMinSize = reader.Read<int>();
				break;
			case "resizeTextMaxSize":
				val.resizeTextMaxSize = reader.Read<int>();
				break;
			case "alignment":
				val.alignment = reader.Read<TextAnchor>();
				break;
			case "alignByGeometry":
				val.alignByGeometry = reader.Read<bool>();
				break;
			case "fontSize":
				val.fontSize = reader.Read<int>();
				break;
			case "horizontalOverflow":
				val.horizontalOverflow = reader.Read<HorizontalWrapMode>();
				break;
			case "verticalOverflow":
				val.verticalOverflow = reader.Read<VerticalWrapMode>();
				break;
			case "lineSpacing":
				val.lineSpacing = reader.Read<float>();
				break;
			case "fontStyle":
				val.fontStyle = reader.Read<FontStyle>();
				break;
			case "onCullStateChanged":
				((MaskableGraphic)val).onCullStateChanged = reader.Read<CullStateChangedEvent>();
				break;
			case "maskable":
				((MaskableGraphic)val).maskable = reader.Read<bool>();
				break;
			case "color":
				((Graphic)val).color = reader.Read<Color>();
				break;
			case "raycastTarget":
				((Graphic)val).raycastTarget = reader.Read<bool>();
				break;
			case "material":
				((Graphic)val).material = reader.Read<Material>();
				break;
			case "useGUILayout":
				((MonoBehaviour)(object)val).useGUILayout = reader.Read<bool>();
				break;
			case "enabled":
				((Behaviour)(object)val).enabled = reader.Read<bool>();
				break;
			case "hideFlags":
				((Object)(object)val).hideFlags = reader.Read<HideFlags>();
				break;
			default:
				reader.Skip();
				break;
			}
		}
	}
}
