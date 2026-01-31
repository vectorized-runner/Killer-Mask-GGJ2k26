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
                Debug.Log($"Camera Pos: {_cameraTransform.position}, Rot: {_cameraTransform.rotation.eulerAngles}");
                Debug.Log($"Target Pos: {_targetTransform.position}, Rot: {_targetTransform.rotation.eulerAngles}");
                Debug.Log($"Rot Angle Diff: {Quaternion.Angle(_cameraTransform.rotation, _targetTransform.rotation)}");
                
                _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, _targetTransform.position, Mathf.Clamp01(Time.deltaTime * _moveSpeed));
                _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, _targetTransform.rotation, Mathf.Clamp01(Time.deltaTime * (_rotateSpeed * 3f)));

                if (Vector3.Distance(_cameraTransform.position, _targetTransform.position) < 0.01f &&
                    Quaternion.Angle(_cameraTransform.rotation, _targetTransform.rotation) < 0.5f)
                {
                    _cameraTransform.position = _targetTransform.position;
                    _cameraTransform.rotation = _targetTransform.rotation;
                    _isMoving = false;
                }
            }
            // TEST: T tuşuna basınca kamerayı anında hedefe döndür
            if (Input.GetKeyDown(KeyCode.T) && _targetTransform != null)
            {
                Debug.Log($"[TEST] _cameraTransform: {_cameraTransform.name}");
                if (_cameraTransform.parent != null)
                {
                    Debug.Log($"[TEST] _cameraTransform parent: {_cameraTransform.parent.name}, parent rot: {_cameraTransform.parent.rotation.eulerAngles}");
                }
                _cameraTransform.rotation = _targetTransform.rotation;
                Debug.Log($"[TEST] T basıldıktan sonra kamera rotasyonu: {_cameraTransform.rotation.eulerAngles}");
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
