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
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var layerMask = LayerMask.GetMask("Mask");
			
			if (Physics.Raycast(ray, out var hit, 100f, layerMask))
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
}