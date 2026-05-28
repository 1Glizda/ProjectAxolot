using System.Collections;
using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Player.Helpers
{
    public class VineHelper : MonoBehaviour
    {

        [SerializeField] private List<SwingBone> _bones;
        public int BoneCount => _bones.Count;

        private Dictionary<GameObject, int> _originalLayers;

        private void Awake()
        {
            _originalLayers = new Dictionary<GameObject, int>();
            SaveLayersRecursively(gameObject);
        }

        private void SaveLayersRecursively(GameObject obj)
        {
            _originalLayers[obj] = obj.layer;
            foreach (Transform child in obj.transform)
            {
                SaveLayersRecursively(child.gameObject);
            }
        }
        
        public int GetBoneIndex(SwingBone swingBone)
        {
            return _bones.FindIndex(b => b == swingBone);
        }

        public SwingBone GetBoneByIndex(int index)
        {
            return _bones[index];
        }
        
        public void SetLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer == -1) return;
            
            SetLayerRecursively(gameObject, layer);
        }

        public void RestoreLayerDelayed(float delaySeconds)
        {
            StartCoroutine(RestoreLayerRoutine(delaySeconds));
        }

        private IEnumerator RestoreLayerRoutine(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            RestoreOriginalLayers();
        }

        private void RestoreOriginalLayers()
        {
            if (_originalLayers == null) return;
            foreach (var kvp in _originalLayers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.layer = kvp.Value;
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}
