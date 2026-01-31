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
		
		private void Start()
		{
			State = GameState.WaitingForStartInput;
		}
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				SceneManager.LoadScene("Game");
			}
		}
	}
}