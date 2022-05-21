using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSkinning
{
    public enum GPUSkinningWrapMode
    {
        Once,
        Loop
    }

    [System.Serializable]
    public class GPUSkinningClip
    {
        public string name = null;
        public float length = 0.0f;
        public int fps = 0;

        /// <summary>
        /// Clip��Ϣ����ͼ�е�λ��(��pixelSegmentation�����ؿ�ʼ)
        /// </summary>
        public int pixelSegmentation = 0;
        public GPUSkinningWrapMode wrapMode = GPUSkinningWrapMode.Once;
        public GPUSkinningFrame[] frames = null;
    }
}