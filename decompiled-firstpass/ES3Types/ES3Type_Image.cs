using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace ES3Types;

[Preserve]
[ES3Properties(new string[]
{
	"sprite", "overrideSprite", "type", "preserveAspect", "fillCenter", "fillMethod", "fillAmount", "fillClockwise", "fillOrigin", "alphaHitTestMinimumThreshold",
	"useSpriteMesh", "pixelsPerUnitMultiplier", "material", "onCullStateChanged", "maskable", "color", "raycastTarget", "useLegacyMeshGeneration", "useGUILayout", "enabled",
	"hideFlags"
})]
public class ES3Type_Image : ES3ComponentType
{
	public static ES3Type Instance;

	public ES3Type_Image()
		: base(typeof(Image))
	{
		Instance = this;
	}

	protected override void WriteComponent(object obj, ES3Writer writer)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		Image val = (Image)obj;
		writer.WritePropertyByRef("sprite", val.sprite);
		writer.WriteProperty("type", val.type);
		writer.WriteProperty("preserveAspect", val.preserveAspect, ES3Type_bool.Instance);
		writer.WriteProperty("fillCenter", val.fillCenter, ES3Type_bool.Instance);
		writer.WriteProperty("fillMethod", val.fillMethod);
		writer.WriteProperty("fillAmount", val.fillAmount, ES3Type_float.Instance);
		writer.WriteProperty("fillClockwise", val.fillClockwise, ES3Type_bool.Instance);
		writer.WriteProperty("fillOrigin", val.fillOrigin, ES3Type_int.Instance);
		writer.WriteProperty("useSpriteMesh", val.useSpriteMesh, ES3Type_bool.Instance);
		if (((Graphic)val).material.name.Contains("Default"))
		{
			writer.WriteProperty("material", null);
		}
		else
		{
			writer.WriteProperty("material", ((Graphic)val).material);
		}
		writer.WriteProperty("onCullStateChanged", ((MaskableGraphic)val).onCullStateChanged);
		writer.WriteProperty("maskable", ((MaskableGraphic)val).maskable, ES3Type_bool.Instance);
		writer.WriteProperty("color", ((Graphic)val).color, ES3Type_Color.Instance);
		writer.WriteProperty("raycastTarget", ((Graphic)val).raycastTarget, ES3Type_bool.Instance);
		writer.WritePrivateProperty("useLegacyMeshGeneration", val);
		writer.WriteProperty("useGUILayout", ((MonoBehaviour)(object)val).useGUILayout, ES3Type_bool.Instance);
		writer.WriteProperty("enabled", ((Behaviour)(object)val).enabled, ES3Type_bool.Instance);
		writer.WriteProperty("hideFlags", ((Object)(object)val).hideFlags, ES3Type_enum.Instance);
	}

	protected override void ReadComponent<T>(ES3Reader reader, object obj)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		Image val = (Image)obj;
		foreach (string property in reader.Properties)
		{
			switch (property)
			{
			case "sprite":
				val.sprite = reader.Read<Sprite>(ES3Type_Sprite.Instance);
				break;
			case "type":
				val.type = reader.Read<Type>();
				break;
			case "preserveAspect":
				val.preserveAspect = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "fillCenter":
				val.fillCenter = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "fillMethod":
				val.fillMethod = reader.Read<FillMethod>();
				break;
			case "fillAmount":
				val.fillAmount = reader.Read<float>(ES3Type_float.Instance);
				break;
			case "fillClockwise":
				val.fillClockwise = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "fillOrigin":
				val.fillOrigin = reader.Read<int>(ES3Type_int.Instance);
				break;
			case "useSpriteMesh":
				val.useSpriteMesh = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "material":
				((Graphic)val).material = reader.Read<Material>(ES3Type_Material.Instance);
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
			case "useGUILayout":
				((MonoBehaviour)(object)val).useGUILayout = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "enabled":
				((Behaviour)(object)val).enabled = reader.Read<bool>(ES3Type_bool.Instance);
				break;
			case "hideFlags":
				((Object)(object)val).hideFlags = reader.Read<HideFlags>(ES3Type_enum.Instance);
				break;
			default:
				reader.Skip();
				break;
			}
		}
	}
}
