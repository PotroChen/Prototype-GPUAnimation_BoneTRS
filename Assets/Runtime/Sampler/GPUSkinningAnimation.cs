using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSkinning
{
    public class GPUSkinningAnimation : ScriptableObject
    {
        public string guid;

        public int rootBoneIndex = 0;
        public GPUSkinningBone[] bones = null;

        public GPUSkinningClip[] clips = null;

        public int textureWidth = 0;

        public int textureHeight = 0;

        //先考虑只有一个SkinnedMeshRenderer 
        //public GPUSkinningSkeleton[] skeletions = null;
        //多个SkinnedMeshRenderer时遇到的问题
        //1.调用CollectBones的时候，需要不依赖SkinnedMeshRenderer指定Transform[] bones_smr和Transform currentBoneTransform(rootbone)
        // 不确定smr.bones返回的Transform[]是否顺序规则
    }
}