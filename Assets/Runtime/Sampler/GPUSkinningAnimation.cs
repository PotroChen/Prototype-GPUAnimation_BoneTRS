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

        //�ȿ���ֻ��һ��SkinnedMeshRenderer 
        //public GPUSkinningSkeleton[] skeletions = null;
        //���SkinnedMeshRendererʱ����������
        //1.����CollectBones��ʱ����Ҫ������SkinnedMeshRendererָ��Transform[] bones_smr��Transform currentBoneTransform(rootbone)
        // ��ȷ��smr.bones���ص�Transform[]�Ƿ�˳�����
    }
}