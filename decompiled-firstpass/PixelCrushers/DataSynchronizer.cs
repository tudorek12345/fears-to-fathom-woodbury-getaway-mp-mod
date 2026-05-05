using UnityEngine;

namespace PixelCrushers;

[AddComponentMenu("")]
public class DataSynchronizer : MonoBehaviour, IMessageHandler
{
	public const string DataSourceValueChangedMessage = "Data Source Value Changed";

	public const string RequestDataSourceChangeValueMessage = "Request Data Source Change Value";

	[Tooltip("A name to associate with the data source. Data change messages that reference this name will invoke the value update events.")]
	[SerializeField]
	private string m_dataSourceName;

	[SerializeField]
	private ObjectUnityEvent m_onRequestDataSourceChangeValue = new ObjectUnityEvent();

	public string dataSourceName
	{
		get
		{
			return m_dataSourceName;
		}
		set
		{
			m_dataSourceName = value;
		}
	}

	public ObjectUnityEvent onRequestDataSourceChangeValue
	{
		get
		{
			return m_onRequestDataSourceChangeValue;
		}
		set
		{
			m_onRequestDataSourceChangeValue = value;
		}
	}

	protected virtual void OnEnable()
	{
		MessageSystem.AddListener(this, "Request Data Source Change Value", dataSourceName);
	}

	protected virtual void OnDisable()
	{
		MessageSystem.RemoveListener(this, "Request Data Source Change Value", dataSourceName);
	}

	public void OnMessage(MessageArgs messageArgs)
	{
		onRequestDataSourceChangeValue.Invoke(messageArgs.firstValue);
	}

	public void DataSourceValueChanged(object newValue)
	{
		MessageSystem.SendMessage(this, "Data Source Value Changed", dataSourceName, newValue);
	}
}
