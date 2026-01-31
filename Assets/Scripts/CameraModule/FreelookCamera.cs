using UnityEngine;
namespace CameraModule
{
	public class FreelookCamera : MonoBehaviour
	{
		[Header("Mouse Settings")]
		public float mouseSensitivity = 150f;
		public bool invertY = false;

		[Header("Rotation Limits")]
		[Tooltip("Up / Down limit")]
		public float minPitch = -60f;
		public float maxPitch = 60f;

		[Tooltip("Left / Right limit relative to start rotation")]
		public float minYaw = -90f;
		public float maxYaw = 90f;

		private float yaw;
		private float pitch;
		private float startYaw;

		void Start()
		{
			// Cache starting yaw so limits are relative
			startYaw = this.transform.eulerAngles.y;
			yaw = startYaw;
			pitch = this.transform.eulerAngles.x;
		}

		void OnEnable()
		{
			// Her enable olduğunda mevcut rotasyonu referans al
			startYaw = this.transform.eulerAngles.y;
			yaw = startYaw;
			pitch = this.transform.eulerAngles.x;
		}

		void Update()
		{
			float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
			float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

			yaw += mouseX;

			if (invertY)
				pitch += mouseY;
			else
				pitch -= mouseY;

			// Clamp pitch (up/down)
			pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

			// Clamp yaw relative to starting direction
			yaw = Mathf.Clamp(yaw, startYaw + minYaw, startYaw + maxYaw);

			this.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
		}
	}
}