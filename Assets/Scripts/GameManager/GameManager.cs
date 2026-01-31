using UnityEngine;

namespace GameManager
{
	public class GameManager : MonoBehaviour
	{
		
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.R))
			{
				SceneManager.LoadScene("Game");
			}
		}
	}
}