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

        private void Awake()
        {
            _toolCollidersDict = new Dictionary<DeskTool, List<MeshCollider>>();
            foreach (var item in ToolCollidersList)
            {
                _toolCollidersDict[item.ToolType] = item.Colliders;
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
            float startY = show ? 10f : 1f;
            float endY = show ? 1f : 10f;
            foreach (var kvp in _toolCollidersDict)
            {
                foreach (var mesh in kvp.Value)
                {
                    StartCoroutine(MoveMesh(mesh.transform, startY, endY, duration));
                }
            }
            yield return new WaitForSeconds(duration);
            _isAnimating = false;
        }

        private IEnumerator MoveMesh(Transform mesh, float fromY, float toY, float duration)
        {
            Vector3 startPos = mesh.position;
            startPos.y = fromY;
            Vector3 endPos = mesh.position;
            endPos.y = toY;
            float elapsed = 0f;
            mesh.position = startPos;
            while (elapsed < duration)
            {
                mesh.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            mesh.position = endPos;
        }
    }
}
