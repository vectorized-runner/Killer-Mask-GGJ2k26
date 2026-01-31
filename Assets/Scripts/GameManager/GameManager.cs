using System;
using System.Collections;
using DG.Tweening;
using KillerLocomotion;
using Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameManager
{
	public enum GameState
	{
		WaitingForStartInput,
		Incoming,
		MaskSetup,
		Outgoing,
	}
	
	public class GameManager : MonoBehaviour
	{
		public GameState State;
		public float KillerComeInDelay = 0.5f;
		public float DoorCloseDelay = 0.5f;
		
		private FreelookCamera _freelookCam;
		private KillerLocomotionController _killer;
		
		private void Start()
		{
			State = GameState.WaitingForStartInput;
			
			_freelookCam = FindFirstObjectByType<FreelookCamera>();
			_killer = FindFirstObjectByType<KillerLocomotionController>();
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
					State = GameState.Incoming;
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
			_freelookCam.enabled = true;

			yield return new WaitUntil(() => State == GameState.Incoming);
			Debug.LogError("State = Incoming");

			var coroutine = StartCoroutine(FindFirstObjectByType<AutoDoorScript>().OpenDoor());
			yield return coroutine;
			

			yield return new WaitForSeconds(KillerComeInDelay);
			
			_killer.StartInComingMovementLocomotion();
			
			yield return new WaitForSeconds(DoorCloseDelay);
			
			StartCoroutine(FindFirstObjectByType<AutoDoorScript>().CloseDoor());

			yield return new WaitUntil(() => !_killer.IncomingMovementSequence.active);
			
			Debug.LogError("Done");
			
			yield return null;
		}
	}
}