using System.Globalization;
using DG.Tweening.Core;
using DG.Tweening.Core.Enums;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.UI;

namespace DG.Tweening;

public static class DOTweenModuleUI
{
	public static class Utils
	{
		public static Vector2 SwitchToRectTransform(RectTransform from, RectTransform to)
		{
			Vector2 vector = new Vector2(from.rect.width * 0.5f + from.rect.xMin, from.rect.height * 0.5f + from.rect.yMin);
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, from.position);
			screenPoint += vector;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(to, screenPoint, null, out var localPoint);
			Vector2 vector2 = new Vector2(to.rect.width * 0.5f + to.rect.xMin, to.rect.height * 0.5f + to.rect.yMin);
			return to.anchoredPosition + localPoint - vector2;
		}
	}

	public static TweenerCore<float, float, FloatOptions> DOFade(this CanvasGroup target, float endValue, float duration)
	{
		TweenerCore<float, float, FloatOptions> obj = DOTween.To((DOGetter<float>)(() => target.alpha), (DOSetter<float>)delegate(float x)
		{
			target.alpha = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<float, float, FloatOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOColor(this Graphic target, Color endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.To((DOGetter<Color>)(() => target.color), (DOSetter<Color>)delegate(Color x)
		{
			target.color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOFade(this Graphic target, float endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.ToAlpha((DOGetter<Color>)(() => target.color), (DOSetter<Color>)delegate(Color x)
		{
			target.color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOColor(this Image target, Color endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.To((DOGetter<Color>)(() => ((Graphic)target).color), (DOSetter<Color>)delegate(Color x)
		{
			((Graphic)target).color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOFade(this Image target, float endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.ToAlpha((DOGetter<Color>)(() => ((Graphic)target).color), (DOSetter<Color>)delegate(Color x)
		{
			((Graphic)target).color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<float, float, FloatOptions> DOFillAmount(this Image target, float endValue, float duration)
	{
		if (endValue > 1f)
		{
			endValue = 1f;
		}
		else if (endValue < 0f)
		{
			endValue = 0f;
		}
		TweenerCore<float, float, FloatOptions> obj = DOTween.To((DOGetter<float>)(() => target.fillAmount), (DOSetter<float>)delegate(float x)
		{
			target.fillAmount = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<float, float, FloatOptions>>(obj, (object)target);
		return obj;
	}

	public static Sequence DOGradientColor(this Image target, Gradient gradient, float duration)
	{
		Sequence val = DOTween.Sequence();
		GradientColorKey[] colorKeys = gradient.colorKeys;
		int num = colorKeys.Length;
		for (int i = 0; i < num; i++)
		{
			GradientColorKey gradientColorKey = colorKeys[i];
			if (i == 0 && gradientColorKey.time <= 0f)
			{
				((Graphic)target).color = gradientColorKey.color;
				continue;
			}
			float duration2 = ((i == num - 1) ? (duration - TweenExtensions.Duration((Tween)(object)val, false)) : (duration * ((i == 0) ? gradientColorKey.time : (gradientColorKey.time - colorKeys[i - 1].time))));
			TweenSettingsExtensions.Append(val, (Tween)(object)TweenSettingsExtensions.SetEase<TweenerCore<Color, Color, ColorOptions>>(target.DOColor(gradientColorKey.color, duration2), (Ease)1));
		}
		TweenSettingsExtensions.SetTarget<Sequence>(val, (object)target);
		return val;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOFlexibleSize(this LayoutElement target, Vector2 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => new Vector2(target.flexibleWidth, target.flexibleHeight)), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.flexibleWidth = x.x;
			target.flexibleHeight = x.y;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOMinSize(this LayoutElement target, Vector2 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => new Vector2(target.minWidth, target.minHeight)), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.minWidth = x.x;
			target.minHeight = x.y;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOPreferredSize(this LayoutElement target, Vector2 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => new Vector2(target.preferredWidth, target.preferredHeight)), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.preferredWidth = x.x;
			target.preferredHeight = x.y;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOColor(this Outline target, Color endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.To((DOGetter<Color>)(() => ((Shadow)target).effectColor), (DOSetter<Color>)delegate(Color x)
		{
			((Shadow)target).effectColor = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOFade(this Outline target, float endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.ToAlpha((DOGetter<Color>)(() => ((Shadow)target).effectColor), (DOSetter<Color>)delegate(Color x)
		{
			((Shadow)target).effectColor = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOScale(this Outline target, Vector2 endValue, float duration)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => ((Shadow)target).effectDistance), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			((Shadow)target).effectDistance = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Vector2, Vector2, VectorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorPos(this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.anchoredPosition), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchoredPosition = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorPosX(this RectTransform target, float endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.anchoredPosition), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchoredPosition = x;
		}, new Vector2(endValue, 0f), duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, (AxisConstraint)2, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorPosY(this RectTransform target, float endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.anchoredPosition), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchoredPosition = x;
		}, new Vector2(0f, endValue), duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, (AxisConstraint)4, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector3, Vector3, VectorOptions> DOAnchorPos3D(this RectTransform target, Vector3 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector3, Vector3, VectorOptions> obj = DOTween.To((DOGetter<Vector3>)(() => target.anchoredPosition3D), (DOSetter<Vector3>)delegate(Vector3 x)
		{
			target.anchoredPosition3D = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector3, Vector3, VectorOptions> DOAnchorPos3DX(this RectTransform target, float endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector3, Vector3, VectorOptions> obj = DOTween.To((DOGetter<Vector3>)(() => target.anchoredPosition3D), (DOSetter<Vector3>)delegate(Vector3 x)
		{
			target.anchoredPosition3D = x;
		}, new Vector3(endValue, 0f, 0f), duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, (AxisConstraint)2, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector3, Vector3, VectorOptions> DOAnchorPos3DY(this RectTransform target, float endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector3, Vector3, VectorOptions> obj = DOTween.To((DOGetter<Vector3>)(() => target.anchoredPosition3D), (DOSetter<Vector3>)delegate(Vector3 x)
		{
			target.anchoredPosition3D = x;
		}, new Vector3(0f, endValue, 0f), duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, (AxisConstraint)4, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector3, Vector3, VectorOptions> DOAnchorPos3DZ(this RectTransform target, float endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector3, Vector3, VectorOptions> obj = DOTween.To((DOGetter<Vector3>)(() => target.anchoredPosition3D), (DOSetter<Vector3>)delegate(Vector3 x)
		{
			target.anchoredPosition3D = x;
		}, new Vector3(0f, 0f, endValue), duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, (AxisConstraint)8, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorMax(this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.anchorMax), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchorMax = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOAnchorMin(this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.anchorMin), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchorMin = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOPivot(this RectTransform target, Vector2 endValue, float duration)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.pivot), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.pivot = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Vector2, Vector2, VectorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOPivotX(this RectTransform target, float endValue, float duration)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.pivot), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.pivot = x;
		}, new Vector2(endValue, 0f), duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, (AxisConstraint)2, false), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOPivotY(this RectTransform target, float endValue, float duration)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.pivot), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.pivot = x;
		}, new Vector2(0f, endValue), duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, (AxisConstraint)4, false), (object)target);
		return obj;
	}

	public static TweenerCore<Vector2, Vector2, VectorOptions> DOSizeDelta(this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, VectorOptions> obj = DOTween.To((DOGetter<Vector2>)(() => target.sizeDelta), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.sizeDelta = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static Tweener DOPunchAnchorPos(this RectTransform target, Vector2 punch, float duration, int vibrato = 10, float elasticity = 1f, bool snapping = false)
	{
		return TweenSettingsExtensions.SetOptions(TweenSettingsExtensions.SetTarget<TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>>(DOTween.Punch((DOGetter<Vector3>)(() => target.anchoredPosition), (DOSetter<Vector3>)delegate(Vector3 x)
		{
			target.anchoredPosition = x;
		}, (Vector3)punch, duration, vibrato, elasticity), (object)target), snapping);
	}

	public static Tweener DOShakeAnchorPos(this RectTransform target, float duration, float strength = 100f, int vibrato = 10, float randomness = 90f, bool snapping = false, bool fadeOut = true, ShakeRandomnessMode randomnessMode = (ShakeRandomnessMode)0)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		return TweenSettingsExtensions.SetOptions(Extensions.SetSpecialStartupMode<TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>>(TweenSettingsExtensions.SetTarget<TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>>(DOTween.Shake((DOGetter<Vector3>)(() => target.anchoredPosition), (DOSetter<Vector3>)delegate(Vector3 x)
		{
			target.anchoredPosition = x;
		}, duration, strength, vibrato, randomness, true, fadeOut, randomnessMode), (object)target), (SpecialStartupMode)2), snapping);
	}

	public static Tweener DOShakeAnchorPos(this RectTransform target, float duration, Vector2 strength, int vibrato = 10, float randomness = 90f, bool snapping = false, bool fadeOut = true, ShakeRandomnessMode randomnessMode = (ShakeRandomnessMode)0)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		return TweenSettingsExtensions.SetOptions(Extensions.SetSpecialStartupMode<TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>>(TweenSettingsExtensions.SetTarget<TweenerCore<Vector3, Vector3[], Vector3ArrayOptions>>(DOTween.Shake((DOGetter<Vector3>)(() => target.anchoredPosition), (DOSetter<Vector3>)delegate(Vector3 x)
		{
			target.anchoredPosition = x;
		}, duration, (Vector3)strength, vibrato, randomness, fadeOut, randomnessMode), (object)target), (SpecialStartupMode)2), snapping);
	}

	public static Sequence DOJumpAnchorPos(this RectTransform target, Vector2 endValue, float jumpPower, int numJumps, float duration, bool snapping = false)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		if (numJumps < 1)
		{
			numJumps = 1;
		}
		float startPosY = 0f;
		float offsetY = -1f;
		bool offsetYSet = false;
		Sequence s = DOTween.Sequence();
		Tween val = (Tween)(object)TweenSettingsExtensions.OnStart<Tweener>(TweenSettingsExtensions.SetLoops<Tweener>(TweenSettingsExtensions.SetRelative<Tweener>(TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetOptions(DOTween.To((DOGetter<Vector2>)(() => target.anchoredPosition), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchoredPosition = x;
		}, new Vector2(0f, jumpPower), duration / (float)(numJumps * 2)), (AxisConstraint)4, snapping), (Ease)6)), numJumps * 2, (LoopType)1), (TweenCallback)delegate
		{
			startPosY = target.anchoredPosition.y;
		});
		TweenSettingsExtensions.SetEase<Sequence>(TweenSettingsExtensions.SetTarget<Sequence>(TweenSettingsExtensions.Join(TweenSettingsExtensions.Append(s, (Tween)(object)TweenSettingsExtensions.SetEase<Tweener>(TweenSettingsExtensions.SetOptions(DOTween.To((DOGetter<Vector2>)(() => target.anchoredPosition), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchoredPosition = x;
		}, new Vector2(endValue.x, 0f), duration), (AxisConstraint)2, snapping), (Ease)1)), val), (object)target), DOTween.defaultEaseType);
		TweenSettingsExtensions.OnUpdate<Sequence>(s, (TweenCallback)delegate
		{
			if (!offsetYSet)
			{
				offsetYSet = true;
				offsetY = (((Tween)s).isRelative ? endValue.y : (endValue.y - startPosY));
			}
			Vector2 anchoredPosition = target.anchoredPosition;
			anchoredPosition.y += DOVirtual.EasedValue(0f, offsetY, TweenExtensions.ElapsedDirectionalPercentage((Tween)(object)s), (Ease)6);
			target.anchoredPosition = anchoredPosition;
		});
		return s;
	}

	public static Tweener DONormalizedPos(this ScrollRect target, Vector2 endValue, float duration, bool snapping = false)
	{
		return TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(DOTween.To((DOGetter<Vector2>)(() => new Vector2(target.horizontalNormalizedPosition, target.verticalNormalizedPosition)), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.horizontalNormalizedPosition = x.x;
			target.verticalNormalizedPosition = x.y;
		}, endValue, duration), snapping), (object)target);
	}

	public static Tweener DOHorizontalNormalizedPos(this ScrollRect target, float endValue, float duration, bool snapping = false)
	{
		return TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(DOTween.To((DOGetter<float>)(() => target.horizontalNormalizedPosition), (DOSetter<float>)delegate(float x)
		{
			target.horizontalNormalizedPosition = x;
		}, endValue, duration), snapping), (object)target);
	}

	public static Tweener DOVerticalNormalizedPos(this ScrollRect target, float endValue, float duration, bool snapping = false)
	{
		return TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(DOTween.To((DOGetter<float>)(() => target.verticalNormalizedPosition), (DOSetter<float>)delegate(float x)
		{
			target.verticalNormalizedPosition = x;
		}, endValue, duration), snapping), (object)target);
	}

	public static TweenerCore<float, float, FloatOptions> DOValue(this Slider target, float endValue, float duration, bool snapping = false)
	{
		TweenerCore<float, float, FloatOptions> obj = DOTween.To((DOGetter<float>)(() => target.value), (DOSetter<float>)delegate(float x)
		{
			target.value = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, snapping), (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOColor(this Text target, Color endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.To((DOGetter<Color>)(() => ((Graphic)target).color), (DOSetter<Color>)delegate(Color x)
		{
			((Graphic)target).color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<int, int, NoOptions> DOCounter(this Text target, int fromValue, int endValue, float duration, bool addThousandsSeparator = true, CultureInfo culture = null)
	{
		CultureInfo cInfo = ((!addThousandsSeparator) ? null : (culture ?? CultureInfo.InvariantCulture));
		TweenerCore<int, int, NoOptions> obj = DOTween.To((DOGetter<int>)(() => fromValue), (DOSetter<int>)delegate(int x)
		{
			fromValue = x;
			target.text = (addThousandsSeparator ? fromValue.ToString("N0", cInfo) : fromValue.ToString());
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<int, int, NoOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOFade(this Text target, float endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.ToAlpha((DOGetter<Color>)(() => ((Graphic)target).color), (DOSetter<Color>)delegate(Color x)
		{
			((Graphic)target).color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<string, string, StringOptions> DOText(this Text target, string endValue, float duration, bool richTextEnabled = true, ScrambleMode scrambleMode = (ScrambleMode)0, string scrambleChars = null)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (endValue == null)
		{
			if (Debugger.logPriority > 0)
			{
				Debugger.LogWarning((object)"You can't pass a NULL string to DOText: an empty string will be used instead to avoid errors", (Tween)null);
			}
			endValue = "";
		}
		TweenerCore<string, string, StringOptions> obj = DOTween.To((DOGetter<string>)(() => target.text), (DOSetter<string>)delegate(string x)
		{
			target.text = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, richTextEnabled, scrambleMode, scrambleChars), (object)target);
		return obj;
	}

	public static Tweener DOBlendableColor(this Graphic target, Color endValue, float duration)
	{
		endValue -= target.color;
		Color to = new Color(0f, 0f, 0f, 0f);
		return (Tweener)(object)TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(Extensions.Blendable<Color, Color, ColorOptions>(DOTween.To((DOGetter<Color>)(() => to), (DOSetter<Color>)delegate(Color x)
		{
			Color color = x - to;
			to = x;
			Graphic obj = target;
			obj.color += color;
		}, endValue, duration)), (object)target);
	}

	public static Tweener DOBlendableColor(this Image target, Color endValue, float duration)
	{
		endValue -= ((Graphic)target).color;
		Color to = new Color(0f, 0f, 0f, 0f);
		return (Tweener)(object)TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(Extensions.Blendable<Color, Color, ColorOptions>(DOTween.To((DOGetter<Color>)(() => to), (DOSetter<Color>)delegate(Color x)
		{
			Color color = x - to;
			to = x;
			Image obj = target;
			((Graphic)obj).color = ((Graphic)obj).color + color;
		}, endValue, duration)), (object)target);
	}

	public static Tweener DOBlendableColor(this Text target, Color endValue, float duration)
	{
		endValue -= ((Graphic)target).color;
		Color to = new Color(0f, 0f, 0f, 0f);
		return (Tweener)(object)TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(Extensions.Blendable<Color, Color, ColorOptions>(DOTween.To((DOGetter<Color>)(() => to), (DOSetter<Color>)delegate(Color x)
		{
			Color color = x - to;
			to = x;
			Text obj = target;
			((Graphic)obj).color = ((Graphic)obj).color + color;
		}, endValue, duration)), (object)target);
	}

	public static TweenerCore<Vector2, Vector2, CircleOptions> DOShapeCircle(this RectTransform target, Vector2 center, float endValueDegrees, float duration, bool relativeCenter = false, bool snapping = false)
	{
		TweenerCore<Vector2, Vector2, CircleOptions> obj = DOTween.To<Vector2, Vector2, CircleOptions>(CirclePlugin.Get(), (DOGetter<Vector2>)(() => target.anchoredPosition), (DOSetter<Vector2>)delegate(Vector2 x)
		{
			target.anchoredPosition = x;
		}, center, duration);
		TweenSettingsExtensions.SetTarget<Tweener>(TweenSettingsExtensions.SetOptions(obj, endValueDegrees, relativeCenter, snapping), (object)target);
		return obj;
	}
}
