using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening;

public static class DOTweenModuleSprite
{
	public static TweenerCore<Color, Color, ColorOptions> DOColor(this SpriteRenderer target, Color endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.To((DOGetter<Color>)(() => target.color), (DOSetter<Color>)delegate(Color x)
		{
			target.color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<Color, Color, ColorOptions> DOFade(this SpriteRenderer target, float endValue, float duration)
	{
		TweenerCore<Color, Color, ColorOptions> obj = DOTween.ToAlpha((DOGetter<Color>)(() => target.color), (DOSetter<Color>)delegate(Color x)
		{
			target.color = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(obj, (object)target);
		return obj;
	}

	public static Sequence DOGradientColor(this SpriteRenderer target, Gradient gradient, float duration)
	{
		Sequence val = DOTween.Sequence();
		GradientColorKey[] colorKeys = gradient.colorKeys;
		int num = colorKeys.Length;
		for (int i = 0; i < num; i++)
		{
			GradientColorKey gradientColorKey = colorKeys[i];
			if (i == 0 && gradientColorKey.time <= 0f)
			{
				target.color = gradientColorKey.color;
				continue;
			}
			float duration2 = ((i == num - 1) ? (duration - TweenExtensions.Duration((Tween)(object)val, false)) : (duration * ((i == 0) ? gradientColorKey.time : (gradientColorKey.time - colorKeys[i - 1].time))));
			TweenSettingsExtensions.Append(val, (Tween)(object)TweenSettingsExtensions.SetEase<TweenerCore<Color, Color, ColorOptions>>(target.DOColor(gradientColorKey.color, duration2), (Ease)1));
		}
		TweenSettingsExtensions.SetTarget<Sequence>(val, (object)target);
		return val;
	}

	public static Tweener DOBlendableColor(this SpriteRenderer target, Color endValue, float duration)
	{
		endValue -= target.color;
		Color to = new Color(0f, 0f, 0f, 0f);
		return (Tweener)(object)TweenSettingsExtensions.SetTarget<TweenerCore<Color, Color, ColorOptions>>(Extensions.Blendable<Color, Color, ColorOptions>(DOTween.To((DOGetter<Color>)(() => to), (DOSetter<Color>)delegate(Color x)
		{
			Color color = x - to;
			to = x;
			target.color += color;
		}, endValue, duration)), (object)target);
	}
}
