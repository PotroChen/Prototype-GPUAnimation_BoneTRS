using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSkinning
{
    /*
     *
     */
    [System.Serializable]
    public class GPUSkinningBone
    {
        public string name;
        public string guid;

        [System.NonSerialized]
        public Transform transform = null;

        public Matrix4x4 bindpose;

        public int parentBoneIndex = -1;

        public int[] childrenBonesIndices = null;
    }
}
