using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PixelCrushers;

[AddComponentMenu("")]
public class UIScrollbarEnabler : MonoBehaviour
{
	[Tooltip("The scroll rect.")]
	[FormerlySerializedAs("container")]
	public ScrollRect scrollRect;

	[Tooltip("The content inside the scroll rect. The scrollbar will be enabled if the content is taller than the scroll rect.")]
	[FormerlySerializedAs("content")]
	public RectTransform scrollContent;

	[Tooltip("The scrollbar to enable or disable. If scroll rect doesn't have a scrollbar, just scrolls scroll rect.")]
	public Scrollbar scrollbar;

	[Tooltip("Scroll smoothly instead of jumping to reset value.")]
	public bool smoothScroll;

	public float smoothScrollSpeed = 5f;

	protected bool m_started;

	protected bool m_checking;

	protected RectTransform m_scrollRectTransform;

	protected virtual void Start()
	{
		m_started = true;
		CheckScrollbar();
	}

	public virtual void OnEnable()
	{
		if (m_started)
		{
			CheckScrollbar();
		}
	}

	public virtual void OnDisable()
	{
		m_checking = false;
	}

	public virtual void CheckScrollbar()
	{
		if (!m_checking && !((Object)(object)scrollRect == null) && !(scrollContent == null) && base.gameObject.activeInHierarchy && base.enabled)
		{
			StopAllCoroutines();
			StartCoroutine(CheckScrollbarAfterUIUpdate(useResetValue: false, 0f));
		}
	}

	public virtual void CheckScrollbarWithResetValue(float value)
	{
		if (!m_checking && !((Object)(object)scrollRect == null) && !(scrollContent == null) && base.gameObject.activeInHierarchy && base.enabled)
		{
			StopAllCoroutines();
			StartCoroutine(CheckScrollbarAfterUIUpdate(useResetValue: true, value));
		}
	}

	protected virtual IEnumerator CheckScrollbarAfterUIUpdate(bool useResetValue, float resetValue)
	{
		m_checking = true;
		yield return null;
		if ((Object)(object)scrollbar != null)
		{
			((Component)(object)scrollbar).gameObject.SetActive(scrollContent.rect.height > ((Component)(object)scrollRect).GetComponent<RectTransform>().rect.height);
		}
		m_checking = false;
		yield return null;
		if (!useResetValue)
		{
			yield break;
		}
		if (smoothScroll)
		{
			float height = scrollContent.rect.height;
			if (m_scrollRectTransform == null)
			{
				m_scrollRectTransform = ((Component)(object)scrollRect).GetComponent<RectTransform>();
			}
			float height2 = m_scrollRectTransform.rect.height;
			if (height > height2)
			{
				float ratio = height2 / height;
				float timeout = Time.time + 10f;
				while (scrollRect.verticalNormalizedPosition > 0.01f && Time.time < timeout)
				{
					float b = scrollRect.verticalNormalizedPosition - smoothScrollSpeed * Time.deltaTime * ratio;
					scrollRect.verticalNormalizedPosition = Mathf.Max(0f, b);
					yield return null;
				}
			}
			scrollRect.verticalNormalizedPosition = 0f;
		}
		else
		{
			if ((Object)(object)scrollbar != null)
			{
				scrollbar.value = resetValue;
			}
			scrollRect.verticalNormalizedPosition = resetValue;
		}
	}
}
