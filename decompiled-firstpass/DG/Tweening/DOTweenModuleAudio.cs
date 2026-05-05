using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Audio;

namespace DG.Tweening;

public static class DOTweenModuleAudio
{
	public static TweenerCore<float, float, FloatOptions> DOFade(this AudioSource target, float endValue, float duration)
	{
		if (endValue < 0f)
		{
			endValue = 0f;
		}
		else if (endValue > 1f)
		{
			endValue = 1f;
		}
		TweenerCore<float, float, FloatOptions> obj = DOTween.To((DOGetter<float>)(() => target.volume), (DOSetter<float>)delegate(float x)
		{
			target.volume = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<float, float, FloatOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<float, float, FloatOptions> DOPitch(this AudioSource target, float endValue, float duration)
	{
		TweenerCore<float, float, FloatOptions> obj = DOTween.To((DOGetter<float>)(() => target.pitch), (DOSetter<float>)delegate(float x)
		{
			target.pitch = x;
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<float, float, FloatOptions>>(obj, (object)target);
		return obj;
	}

	public static TweenerCore<float, float, FloatOptions> DOSetFloat(this AudioMixer target, string floatName, float endValue, float duration)
	{
		TweenerCore<float, float, FloatOptions> obj = DOTween.To((DOGetter<float>)delegate
		{
			target.GetFloat(floatName, out var value);
			return value;
		}, (DOSetter<float>)delegate(float x)
		{
			target.SetFloat(floatName, x);
		}, endValue, duration);
		TweenSettingsExtensions.SetTarget<TweenerCore<float, float, FloatOptions>>(obj, (object)target);
		return obj;
	}

	public static int DOComplete(this AudioMixer target, bool withCallbacks = false)
	{
		return DOTween.Complete((object)target, withCallbacks);
	}

	public static int DOKill(this AudioMixer target, bool complete = false)
	{
		return DOTween.Kill((object)target, complete);
	}

	public static int DOFlip(this AudioMixer target)
	{
		return DOTween.Flip((object)target);
	}

	public static int DOGoto(this AudioMixer target, float to, bool andPlay = false)
	{
		return DOTween.Goto((object)target, to, andPlay);
	}

	public static int DOPause(this AudioMixer target)
	{
		return DOTween.Pause((object)target);
	}

	public static int DOPlay(this AudioMixer target)
	{
		return DOTween.Play((object)target);
	}

	public static int DOPlayBackwards(this AudioMixer target)
	{
		return DOTween.PlayBackwards((object)target);
	}

	public static int DOPlayForward(this AudioMixer target)
	{
		return DOTween.PlayForward((object)target);
	}

	public static int DORestart(this AudioMixer target)
	{
		return DOTween.Restart((object)target, true, -1f);
	}

	public static int DORewind(this AudioMixer target)
	{
		return DOTween.Rewind((object)target, true);
	}

	public static int DOSmoothRewind(this AudioMixer target)
	{
		return DOTween.SmoothRewind((object)target);
	}

	public static int DOTogglePause(this AudioMixer target)
	{
		return DOTween.TogglePause((object)target);
	}
}
