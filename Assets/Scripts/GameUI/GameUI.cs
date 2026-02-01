using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
	public TextMeshProUGUI Text;
	public Image Cursor;

	private void Update()
	{
		Cursor.rectTransform.anchoredPosition = Input.mousePosition;
	}
}