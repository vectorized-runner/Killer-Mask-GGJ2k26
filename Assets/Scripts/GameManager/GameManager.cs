using System;
using CameraModule;
using System.Collections;
using DeskModule;
using KillerLocomotion;
using Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
	WaitingForStartInput,
	KillerIncoming,
	TableSetup,
	MaskCarving,
	MaskOn,
	KillerOutgoing,
	End,
}

public class GameManager : MonoBehaviour
{
	public GameState State;
	public float KillerComeInDelay = 0.5f;
	public float DoorCloseDelay = 0.5f;
	public float MaskEditDelay = 1.0f;
	public float MoveOutDelay = 5.0f;

	private KillerLocomotionController _killer;
	private MaskCarvingModule _carvingModule;
	private CameraManager _cameraManager;
	private MaskManager _maskManager;
	private DeskTool _currentDeskTool;

	public static GameManager Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		_maskManager = FindFirstObjectByType<MaskManager>();
		_carvingModule = FindFirstObjectByType<MaskCarvingModule>();
	}

	public void SelectDeskTool(DeskTool tool)
	{
		if (State != GameState.MaskCarving)
		{
			return;
		}

		if (_currentDeskTool == tool)
		{
			return;
		}

		Debug.LogError($"Set Tool to {tool}");
		
		switch (tool)
		{
			case DeskTool.Knife:
			{
				_carvingModule.SetMask(_maskManager.InitialMask);
				_carvingModule.CarvingMode = CarvingMode.Carve;
				break;
			}
			case DeskTool.Brush:
			{
				_carvingModule.CarvingMode = CarvingMode.Disabled;
				break;
			}
			case DeskTool.Decal:
			{
				_carvingModule.CarvingMode = CarvingMode.Disabled;
				break;
			}
			case DeskTool.None:
			default:
				throw new ArgumentOutOfRangeException(nameof(tool), tool, null);
		}
	}

	private void Start()
	{
		State = GameState.WaitingForStartInput;

		_killer = FindFirstObjectByType<KillerLocomotionController>();
		_cameraManager = FindFirstObjectByType<CameraManager>();
		StartCoroutine(GameLoop());
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			SceneManager.LoadScene("Game");
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			if (State == GameState.WaitingForStartInput)
			{
				State = GameState.KillerIncoming;
			}

			if (State == GameState.MaskCarving)
			{
				State = GameState.MaskOn;
			}
		}

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			Time.timeScale = 1.0f;
		}

		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			Time.timeScale = 2.0f;
		}

		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			Time.timeScale = 3.0f;
		}
	}

	private IEnumerator GameLoop()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		yield return new WaitUntil(() => State == GameState.KillerIncoming);

		FindFirstObjectByType<GameUI>().gameObject.SetActive(false);

		var coroutine = StartCoroutine(FindFirstObjectByType<AutoDoorScript>().OpenDoor());
		yield return coroutine;

		yield return new WaitForSeconds(KillerComeInDelay);

		_killer.StartInComingMovementLocomotion();

		yield return new WaitForSeconds(DoorCloseDelay);

		StartCoroutine(FindFirstObjectByType<AutoDoorScript>().CloseDoor());

		yield return new WaitUntil(() => !_killer.IncomingMovementSequence.active);

		State = GameState.TableSetup;

		_cameraManager.MoveToPosition(CameraPositionType.MaskEditing);
		yield return new WaitForSeconds(MaskEditDelay);

		State = GameState.MaskCarving;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		var deskModule = FindFirstObjectByType<DeskModuleController>();
		deskModule.EnableDeskModule();

		yield return new WaitUntil(() => State == GameState.MaskOn);
		
		_cameraManager.MoveToPosition(CameraPositionType.Default);
		
		_killer.PlayMaskEquipAnimation(_maskManager.InitialMask);
		
		Debug.LogError("Mask On State");
		yield return new WaitForSeconds(MoveOutDelay);
		
		State = GameState.KillerOutgoing;
		_killer.StartOutGoingMovementLocomotion();

		yield return new WaitUntil(() => !_killer.OutgoingMovementSequence.active);

		Debug.LogError("Done");

		yield return null;
	}
}