using UnityEngine;

public class MaskMaterialSetter : MonoBehaviour
{
	public int CurrentMaterialIndex;
	public bool IsEnabled { get; set; }

	private GameObject Mask;

	private void Awake()
	{
		Mask = FindFirstObjectByType<InitialMask>().gameObject;
	}

	private void Update()
	{
		if(!IsEnabled)
		{
			return;
		}
		
		if (Input.GetMouseButtonDown(0))
		{
			CurrentMaterialIndex++;
			if (CurrentMaterialIndex == GameConfig.Instance.MaskMaterials.Length)
			{
				CurrentMaterialIndex = 0;
			}

			Mask.GetComponentInChildren<Renderer>().material = GameConfig.Instance.MaskMaterials[CurrentMaterialIndex];
		}
	}
}