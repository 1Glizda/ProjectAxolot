using System.Collections.Generic;
using UnityEngine;

namespace Platforming
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
        
        
    }
}
