using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeskModule
{
    public class DeskModuleController : MonoBehaviour
    {
        public enum DeskTool
        {
            Knife,
            Brush,
            Decal,
        }

        // Tool'a karşılık gelen mesh collider listeleri
        [Serializable]
        public class ToolColliders
        {
            public DeskTool ToolType;
            public List<MeshCollider> Colliders;
        }
        public List<ToolColliders> ToolCollidersList;
        private Dictionary<DeskTool, List<MeshCollider>> _toolCollidersDict;

        // Tool seçildiğinde tetiklenen event
        public event Action<DeskTool> OnToolSelected;
        private bool _isEnabled;
        private bool _isAnimating;

        public GameObject MaskObject;

        private Dictionary<Transform, Vector3> _originalPositions = new Dictionary<Transform, Vector3>();

        private void Awake()
        {
            _toolCollidersDict = new Dictionary<DeskTool, List<MeshCollider>>();
            foreach (var item in ToolCollidersList)
            {
                _toolCollidersDict[item.ToolType] = item.Colliders;
            }
            // Orijinal pozisyonları cache'le ve objeleri yukarıya kaldır
            foreach (var kvp in _toolCollidersDict)
            {
                foreach (var mesh in kvp.Value)
                {
                    if (!_originalPositions.ContainsKey(mesh.transform))
                    {
                        _originalPositions[mesh.transform] = mesh.transform.position;
                        mesh.transform.position += Vector3.up * 9f;
                    }
                }
            }
            if (MaskObject != null)
            {
                if (!_originalPositions.ContainsKey(MaskObject.transform))
                {
                    _originalPositions[MaskObject.transform] = MaskObject.transform.position;
                    MaskObject.transform.position += Vector3.up * 9f;
                }
            }
        }

        private void Update()
        {
            if (!_isEnabled || _isAnimating) return;
            if (Input.GetMouseButtonDown(0))
            {
                if (Camera.main == null) return;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    foreach (var kvp in _toolCollidersDict)
                    {
                        if (kvp.Value.Contains(hit.collider as MeshCollider))
                        {
                            OnToolSelected?.Invoke(kvp.Key);
                            break;
                        }
                    }
                }
            }
        }

        public void EnableDeskModule()
        {
            _isEnabled = true;
            StartCoroutine(AnimateTools(true));
        }

        public void DisableDeskModule()
        {
            _isEnabled = false;
            StartCoroutine(AnimateTools(false));
        }

        private IEnumerator AnimateTools(bool show)
        {
            _isAnimating = true;
            float duration = 0.5f;
            foreach (var kvp in _toolCollidersDict)
            {
                foreach (var mesh in kvp.Value)
                {
                    Vector3 from = show ? mesh.transform.position : _originalPositions[mesh.transform];
                    Vector3 to = show ? _originalPositions[mesh.transform] : _originalPositions[mesh.transform] + Vector3.up * 9f;
                    StartCoroutine(MoveMesh(mesh.transform, from, to, duration));
                }
            }
            if (MaskObject != null)
            {
                Vector3 from = show ? MaskObject.transform.position : _originalPositions[MaskObject.transform];
                Vector3 to = show ? _originalPositions[MaskObject.transform] : _originalPositions[MaskObject.transform] + Vector3.up * 9f;
                StartCoroutine(MoveMesh(MaskObject.transform, from, to, duration));
            }
            yield return new WaitForSeconds(duration);
            _isAnimating = false;
        }

        private IEnumerator MoveMesh(Transform mesh, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            mesh.position = from;
            while (elapsed < duration)
            {
                mesh.position = Vector3.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            mesh.position = to;
        }
    }
}
