using UnityEngine;
using System.Collections.Generic;

namespace CameraModule
{
    public enum CameraPositionType
    {
        Default,
        MaskEditing,
    }

    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [System.Serializable]
        public struct CameraPosition
        {
            public CameraPositionType Type;
            public Transform TargetTransform;
        }

        [SerializeField]
        private Transform _cameraTransform;
        [SerializeField]
        private List<CameraPosition> _cameraPositions = new List<CameraPosition>();

        [SerializeField]
        private float _moveSpeed = 3f;
        [SerializeField]
        private float _rotateSpeed = 3f;

        [SerializeField]
        private FreelookCamera _freelookCamera; // Inspector'dan atanacak

        private Transform _targetTransform;
        private bool _isMoving = false;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            if (_isMoving && _targetTransform != null)
            {
                if (_freelookCamera != null && _freelookCamera.enabled)
                    _freelookCamera.enabled = false;
                _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, _targetTransform.position, Mathf.Clamp01(Time.deltaTime * _moveSpeed));
                _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, _targetTransform.rotation, Mathf.Clamp01(Time.deltaTime * (_rotateSpeed * 3f)));

                if (Vector3.Distance(_cameraTransform.position, _targetTransform.position) < 0.01f &&
                    Quaternion.Angle(_cameraTransform.rotation, _targetTransform.rotation) < 0.5f)
                {
                    _cameraTransform.position = _targetTransform.position;
                    _cameraTransform.rotation = _targetTransform.rotation;
                    _isMoving = false;
                    if (_freelookCamera != null && !_freelookCamera.enabled)
                        _freelookCamera.enabled = true;
                }
            }
        }

        public void MoveToPosition(CameraPositionType type)
        {
            foreach (var camPos in _cameraPositions)
            {
                if (camPos.Type == type && camPos.TargetTransform != null)
                {
                    _targetTransform = camPos.TargetTransform;
                    _isMoving = true;
                    return;
                }
            }
            Debug.LogWarning($"Camera position for type {type} not found or not assigned.");
        }
    }
}
