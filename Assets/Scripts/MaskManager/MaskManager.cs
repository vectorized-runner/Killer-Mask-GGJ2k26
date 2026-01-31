using UnityEngine;

public class MaskManager : MonoBehaviour
{
	public GameObject CurrentMask;

	private void Start()
	{
		CurrentMask = FindFirstObjectByType<InitialMask>().gameObject;
		Debug.Assert(CurrentMask != null);
	}
}