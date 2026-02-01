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

	public Texture2D CursorTex;
	
	private KillerLocomotionController _killer;
	private MaskCarvingModule _carvingModule;
	private CameraManager _cameraManager;
	private MaskManager _maskManager;
	private DeskTool _currentDeskTool;
	private TexturePaintingModule _texPaintModule;
	private GameUI _gameUI;
	private int _currentMaskMaterialIndex;
	private MaskMaterialSetter _maskMaterialSetter;
	
	public static GameManager Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
		_maskManager = FindFirstObjectByType<MaskManager>();
		_carvingModule = FindFirstObjectByType<MaskCarvingModule>();
		_texPaintModule = FindFirstObjectByType<TexturePaintingModule>();
		_maskMaterialSetter = FindFirstObjectByType<MaskMaterialSetter>();
		_gameUI = FindAnyObjectByType<GameUI>();
		
		var hotspot = new Vector2(CursorTex.width / 2, CursorTex.height / 2);
		Cursor.SetCursor(CursorTex, hotspot, CursorMode.Auto);
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

		Debug.Log($"Set Tool to {tool}");
		
		switch (tool)
		{
			case DeskTool.Knife:
			{
				_carvingModule.SetMask(_maskManager.InitialMask);
				_carvingModule.CarvingMode = CarvingMode.Carve;

				_texPaintModule.IsEnabled = false;

				_maskMaterialSetter.IsEnabled = false;
				break;
			}
			case DeskTool.Brush:
			{
				_carvingModule.CarvingMode = CarvingMode.Disabled;
				
				_texPaintModule.IsEnabled = true;

				_maskMaterialSetter.IsEnabled = false;
				break;
			}
			case DeskTool.Decal:
			{
				_carvingModule.CarvingMode = CarvingMode.Disabled;

				_texPaintModule.IsEnabled = false;

				_maskMaterialSetter.IsEnabled = true;
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

		_gameUI.Text.enabled = false;
		
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
		
		Cursor.visible = true;

		State = GameState.MaskCarving;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		var deskModule = FindFirstObjectByType<DeskModuleController>();
		deskModule.EnableDeskModule();

		yield return new WaitUntil(() => State == GameState.MaskOn);
		
		_cameraManager.MoveToPosition(CameraPositionType.Default);
		
		_killer.PlayMaskEquipAnimation(_maskManager.InitialMask);
		
		yield return new WaitForSeconds(MoveOutDelay);
		
		State = GameState.KillerOutgoing;
		_killer.StartOutGoingMovementLocomotion();

		yield return new WaitUntil(() => !_killer.OutgoingMovementSequence.active);


		yield return null;
	}
}