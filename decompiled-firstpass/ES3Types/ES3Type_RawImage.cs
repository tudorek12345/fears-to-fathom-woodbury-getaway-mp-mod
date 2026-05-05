using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[]
{
	"texture", "uvRect", "onCullStateChanged", "maskable", "color", "raycastTarget", "useLegacyMeshGeneration", "material", "useGUILayout", "enabled",
	"hideFlags"
})]
public class ES3Type_RawImage : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3Type_RawImage()
		: base(typeof(RawImage))
	{
		Instance = this;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		RawImage val = (RawImage)obj;
		writer.WritePropertyByRef("texture", val.texture);
		writer.WriteProperty("uvRect", val.uvRect, ES3Type_Rect.Instance);
		writer.WriteProperty("onCullStateChanged", ((MaskableGraphic)val).onCullStateChanged);
		writer.WriteProperty("maskable", ((MaskableGraphic)val).maskable, ES3Type_bool.Instance);
		writer.WriteProperty("color", ((Graphic)val).color, ES3Type_Color.Instance);
		writer.WriteProperty("raycastTarget", ((Graphic)val).raycastTarget, ES3Type_bool.Instance);
		writer.WritePrivateProperty("useLegacyMeshGeneration", val);
		if (((Graphic)val).material.name.Contains("Default"))
		{
			writer.WriteProperty("material", null);
		}
		else
		{
			writer.WriteProperty("material", ((Graphic)val).material);
		}
		writer.WriteProperty("useGUILayout", ((MonoBehaviour)(object)val).useGUILayout, ES3Type_bool.Instance);
		writer.WriteProperty("enabled", ((Behaviour)(object)val).enabled, ES3Type_bool.Instance);
		writer.WriteProperty("hideFlags", ((Object)(object)val).hideFlags);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		RawImage val = (RawImage)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "texture":
				val.texture = reader.Read<Texture>(ES3Type_Texture.Instance);
				break;
			case "uvRect":
				val.uvRect = reader.Read<Rect>(ES3Type_Rect.Instance);
				break;
			case "onCullStateChanged":
				((MaskableGraphic)val).onCullStateChanged = reader.Read<CullStateChangedEvent>();
				break;
			case "maskable":
				((MaskableGraphic)val).maskable = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "color":
				((Graphic)val).color = reader.Read<Color>(ES3Type_Color.Instance);
				break;
			case "raycastTarget":
				((Graphic)val).raycastTarget = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "useLegacyMeshGeneration":
				reader.SetPrivateProperty("useLegacyMeshGeneration", reader.Read<bool>(), val);
				break;
			case "material":
				((Graphic)val).material = reader.Read<Material>(ES3Type_Material.Instance);
				break;
			case "useGUILayout":
				((MonoBehaviour)(object)val).useGUILayout = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "enabled":
				((Behaviour)(object)val).enabled = reader.Read<bool>(ES3Type_bool.Instance);
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
