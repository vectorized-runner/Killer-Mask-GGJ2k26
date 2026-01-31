using System;
using System.Collections;
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
		
		private FreelookCamera _freelookCam;
		
		private void Start()
		{
			State = GameState.WaitingForStartInput;
			
			_freelookCam = FindFirstObjectByType<FreelookCamera>();
			StartCoroutine(GameLoop());
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				SceneManager.LoadScene("Game");
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
			
			yield return null;
		}
	}
}