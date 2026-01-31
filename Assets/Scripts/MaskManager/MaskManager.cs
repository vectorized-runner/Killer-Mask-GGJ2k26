using UnityEngine;

namespace MaskManager
{
	public class MaskManager : MonoBehaviour
	{
		private GameObject _initialMask;

		private void Start()
		{
			_initialMask = FindFirstObjectByType<InitialMask>().gameObject;
			Debug.Assert(_initialMask != null);
		}
	}
}