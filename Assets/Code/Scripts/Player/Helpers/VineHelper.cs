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

        public void RestoreLayerDelayed(string layerName, float delaySeconds)
        {
            StartCoroutine(RestoreLayerRoutine(layerName, delaySeconds));
        }

        private IEnumerator RestoreLayerRoutine(string layerName, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            SetLayer(layerName);
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
