using UnityEngine;

public class InputManager : MonoBehaviour
{
	public delegate void InputControl();

	public event InputControl OnInteract;

	public event InputControl OnThrow;

	public event InputControl OnZoom;

	public event InputControl OnAnyKey;

	public event InputControl OnGetUp;

	public event InputControl OnEsc;

	public event InputControl OnTestKey;

	public event InputControl OnInteractPhone;

	public event InputControl OnFlashlight;

	private void Update()
	{
		if (Input.GetMouseButtonDown(0) && this.OnInteract != null)
		{
			this.OnInteract();
		}
		if (Input.GetKeyDown(KeyCode.G) && this.OnThrow != null)
		{
			this.OnThrow();
		}
		if (Input.GetMouseButtonDown(1) && this.OnZoom != null)
		{
			this.OnZoom();
		}
		if (Input.GetKeyDown(KeyCode.Space) && this.OnGetUp != null)
		{
			this.OnGetUp();
		}
		if (Input.GetKeyDown(KeyCode.Escape) && this.OnEsc != null)
		{
			this.OnEsc();
		}
		if (Input.anyKeyDown && this.OnAnyKey != null)
		{
			this.OnAnyKey();
		}
		if (Input.GetKeyDown(KeyCode.L) && this.OnTestKey != null)
		{
			this.OnTestKey();
		}
		if (Input.GetKeyDown(KeyCode.Escape) && this.OnInteractPhone != null)
		{
			this.OnInteractPhone();
		}
		if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.R) && ChecklistAccess.IsGlobalTestingEnabled())
		{
			SceneController.RestartScene();
		}
		if (Input.GetKeyDown(KeyCode.F) && this.OnFlashlight != null)
		{
			this.OnFlashlight();
		}
	}
}
