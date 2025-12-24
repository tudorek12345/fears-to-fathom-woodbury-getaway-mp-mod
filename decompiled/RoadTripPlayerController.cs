using System;
using System.Collections;
using UnityEngine;

public class RoadTripPlayerController : PlayerController
{
	[SerializeField]
	private Camera camera;

	[SerializeField]
	private RoadTripGameManager roadTripGameManager;

	[SerializeField]
	private float turnToMikeSpeed;

	[SerializeField]
	private float zoomToMikeDuration = 2f;

	[SerializeField]
	private Transform mikeTransform;

	[SerializeField]
	private Transform busTransform;

	[SerializeField]
	private Transform deerTransform;

	[SerializeField]
	private float mikeZoom = 40f;

	[SerializeField]
	private DrivingCam drivingCam;

	[SerializeField]
	private FovZoom fovZoom;

	private Transform lookAtObject;

	private float defaultZoom;

	[SerializeField]
	private PerlinCameraShake perlinCameraShake;

	[SerializeField]
	private Camera dialogueCamera;

	private bool zoomIntoTransform;

	private bool returnToDefaultZoom;

	private new void Awake()
	{
		defaultZoom = camera.fieldOfView;
	}

	private void FixedUpdate()
	{
		if (zoomIntoTransform)
		{
			camera.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, mikeZoom, Time.deltaTime * turnToMikeSpeed);
			Vector3 forward = lookAtObject.position - camera.transform.position;
			camera.transform.rotation = Quaternion.Lerp(camera.transform.rotation, Quaternion.LookRotation(forward), Time.deltaTime * turnToMikeSpeed);
		}
		if (returnToDefaultZoom)
		{
			camera.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, defaultZoom, Time.deltaTime * turnToMikeSpeed);
		}
	}

	public void SetCameraForPlayer(LookAtFromCar.CameraLookAtItems cameraLookAtItem, Action cameraSetupCompleteCallBack = null, bool stopCameraLookAt = false)
	{
		perlinCameraShake.StopTrauma();
		switch (cameraLookAtItem)
		{
		case LookAtFromCar.CameraLookAtItems.Mike:
			lookAtObject = mikeTransform;
			break;
		case LookAtFromCar.CameraLookAtItems.Bus:
			lookAtObject = busTransform;
			break;
		case LookAtFromCar.CameraLookAtItems.Deer:
			lookAtObject = deerTransform;
			break;
		}
		returnToDefaultZoom = false;
		FreezeCameraScripts(value: true);
		dialogueCamera.gameObject.SetActive(value: true);
		perlinCameraShake.RestartTrauma();
		cameraSetupCompleteCallBack?.Invoke();
		zoomIntoTransform = true;
		if (stopCameraLookAt)
		{
			StartCoroutine(StopLookAtForObjectIn(cameraLookAtItem, 2f));
		}
	}

	private IEnumerator StopLookAtForObjectIn(LookAtFromCar.CameraLookAtItems cameraLookAt, float seconds)
	{
		yield return new WaitForSeconds(seconds);
		ResumeCameraControlFrom(cameraLookAt);
	}

	public void ResumeCameraControlFrom(LookAtFromCar.CameraLookAtItems lookAtItem)
	{
		ResumeCameraInput();
		switch (lookAtItem)
		{
		case LookAtFromCar.CameraLookAtItems.Mike:
			drivingCam.ResumeRotationFromMike();
			break;
		case LookAtFromCar.CameraLookAtItems.Bus:
			drivingCam.ResumeRotationFromBus();
			break;
		case LookAtFromCar.CameraLookAtItems.Deer:
			drivingCam.ResumeRotationFromDeer();
			break;
		}
		FreezeCameraScripts(value: false);
	}

	public void ResumeCameraInput()
	{
		camera.transform.localRotation = Quaternion.identity;
		dialogueCamera.gameObject.SetActive(value: false);
		zoomIntoTransform = false;
		returnToDefaultZoom = true;
	}

	public void ToggleCameraInput()
	{
		drivingCam.FreezeCam = !drivingCam.FreezeCam;
		fovZoom.disableFov = !fovZoom.disableFov;
	}

	private void FreezeCameraScripts(bool value)
	{
		drivingCam.FreezeCam = value;
		fovZoom.disableFov = value;
	}

	public void ShakeCamera()
	{
		StartCoroutine(perlinCameraShake.BurstShake(0.9f));
	}
}
