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

        public Material OutlineMaterialTemplate; // Inspector'dan atanacak, URPOutlineShader kullanmalı

        private Dictionary<Transform, Vector3> _originalPositions = new Dictionary<Transform, Vector3>();
        private Dictionary<MeshCollider, Material[]> _originalMaterials = new();
        private Dictionary<MeshCollider, Material> _outlineMaterials = new();
        private MeshCollider _hoveredCollider = null;
        private MeshCollider _selectedCollider = null;

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
                        mesh.transform.position += Vector3.up;
                    }
                    // Outline materyalini 1. indexe ekle
                    var meshRenderer = mesh.GetComponent<MeshRenderer>();
                    if (meshRenderer != null && OutlineMaterialTemplate != null)
                    {
                        var originalMats = meshRenderer.sharedMaterials;
                        _originalMaterials[mesh] = originalMats;
                        Material[] newMats;
                        if (originalMats.Length == 1)
                        {
                            newMats = new Material[2];
                            newMats[0] = originalMats[0];
                            newMats[1] = new Material(OutlineMaterialTemplate);
                        }
                        else
                        {
                            newMats = new Material[originalMats.Length];
                            Array.Copy(originalMats, newMats, originalMats.Length);
                            if (newMats.Length > 1)
                                newMats[1] = new Material(OutlineMaterialTemplate);
                        }
                        newMats[1].SetFloat("_OutlineEnabled", 0f);
                        meshRenderer.materials = newMats;
                        _outlineMaterials[mesh] = newMats[1];
                    }
                }
            }
            if (MaskObject != null)
            {
                if (!_originalPositions.ContainsKey(MaskObject.transform))
                {
                    _originalPositions[MaskObject.transform] = MaskObject.transform.position;
                    MaskObject.transform.position += Vector3.up;
                }
            }
        }

        private void Update()
        {
            if (!_isEnabled || _isAnimating) return;
            // Mouse hover kontrolü
            MeshCollider hovered = null;
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hovered = hit.collider as MeshCollider;
                }
            }
            if (_hoveredCollider != hovered)
            {
                if (_hoveredCollider != null && _hoveredCollider != _selectedCollider)
                    SetOutline(_hoveredCollider, false, Color.clear);
                _hoveredCollider = hovered;
                if (_hoveredCollider != null && _hoveredCollider != _selectedCollider)
                    SetOutline(_hoveredCollider, true, Color.blue);
            }
            // Mouse click kontrolü
            if (Input.GetMouseButtonDown(0) && _hoveredCollider != null)
            {
                foreach (var kvp in _toolCollidersDict)
                {
                    if (kvp.Value.Contains(_hoveredCollider))
                    {
                        _selectedCollider = _hoveredCollider;
                        SetOutline(_selectedCollider, true, Color.yellow);
                        OnToolSelected?.Invoke(kvp.Key);
                        break;
                    }
                }
            }
            // Seçili collider dışında kalanların outline'ını kapat
            foreach (var kvp in _outlineMaterials)
            {
                if (kvp.Key != _hoveredCollider && kvp.Key != _selectedCollider)
                    SetOutline(kvp.Key, false, Color.clear);
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
            float delay = 0.25f;
            // Tool meshlerini sırayla animasyonla indir/kaldır, aralarda delay
            foreach (var kvp in _toolCollidersDict)
            {
                foreach (var mesh in kvp.Value)
                {
                    Vector3 from = show ? mesh.transform.position : _originalPositions[mesh.transform];
                    Vector3 to = show ? _originalPositions[mesh.transform] : _originalPositions[mesh.transform] + Vector3.up;
                    StartCoroutine(MoveMesh(mesh.transform, from, to, duration));
                    yield return new WaitForSeconds(delay);
                }
            }
            // Mask objesini en son sırayla indir/kaldır, arada delay
            if (MaskObject != null)
            {
                Vector3 from = show ? MaskObject.transform.position : _originalPositions[MaskObject.transform];
                Vector3 to = show ? _originalPositions[MaskObject.transform] : _originalPositions[MaskObject.transform] + Vector3.up; 
                StartCoroutine(MoveMesh(MaskObject.transform, from, to, duration));
                yield return new WaitForSeconds(delay);
            }
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

        private void SetOutline(MeshCollider mesh, bool enabled, Color color)
        {
            if (_outlineMaterials.TryGetValue(mesh, out var mat))
            {
                mat.SetFloat("_OutlineEnabled", enabled ? 1f : 0f);
                if (enabled && color != Color.clear)
                    mat.SetColor("_OutlineColor", color);
            }
        }
    }
}
