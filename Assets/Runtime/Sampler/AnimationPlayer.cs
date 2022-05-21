using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSkinning
{
    [ExecuteInEditMode]
    public class AnimationPlayer : MonoBehaviour
    {
        public static readonly int PixelSegmentationID = Shader.PropertyToID("_PixelSegmentation");
        public static readonly int AnimationMapInfoID = Shader.PropertyToID("_AnimationMapInfo");
        public bool isPlaying { get; private set; } = false;
        public float time { get; private set; } = 0f;

        [SerializeField]
        private GPUSkinningAnimation m_AnimationAsset;
        private MeshRenderer m_Renderer;
        private GPUSkinningClip m_PlayingClip;
        private MaterialPropertyBlock mpb;

        public void Play(string clipName)
        {
            GPUSkinningClip clip = null;
            int clipCount = (m_AnimationAsset == null || m_AnimationAsset.clips == null) ? 
                    0 : m_AnimationAsset.clips.Length;
            for (int i = 0; i < clipCount; i++)
            {
                if (m_AnimationAsset.clips[i].name == clipName)
                {
                    clip = m_AnimationAsset.clips[i];
                    break;
                }
            }

            if (clip == null)
            {
                Debug.LogFormat("Can not find clip {0}", clipName);
                return;
            }

            Play_Internal(clip);
        }

        private void Play_Internal(GPUSkinningClip clip)
        {
            m_PlayingClip = clip;
            time = 0f;
            isPlaying = true;
        }

        public void Stop()
        {
            OnStopped();
        }

        private void OnStopped()
        {
            m_PlayingClip = null;
            time = 0f;
            isPlaying = false;
        }

        private void Awake()
        {
            m_Renderer = GetComponentInChildren<MeshRenderer>();
            mpb = new MaterialPropertyBlock();
            if (m_AnimationAsset!=null)
            {
                mpb.SetVector(AnimationMapInfoID, new Vector4(
                    m_AnimationAsset.textureWidth,
                    m_AnimationAsset.textureHeight, 
                    m_AnimationAsset.bones.Length * 3));//每三个像素代表每个骨骼在模型空间下的TRS矩阵
                m_Renderer.SetPropertyBlock(mpb);
            }
        }

        private void Update()
        {
            if (!isPlaying)
                return;

            time += Time.deltaTime;

            int frameIndex = GetFrameIndex(m_PlayingClip, time);

            mpb.SetVector(AnimationMapInfoID, new Vector4(
                     m_AnimationAsset.textureWidth,
                     m_AnimationAsset.textureHeight,
                     m_AnimationAsset.bones.Length * 3));//每三个像素代表每个骨骼在模型空间下的TRS矩阵
            mpb.SetVector(PixelSegmentationID, new Vector4(frameIndex, m_PlayingClip.pixelSegmentation, 0f, 0f));
            m_Renderer.SetPropertyBlock(mpb);

            if (m_PlayingClip.wrapMode == GPUSkinningWrapMode.Once && time >= m_PlayingClip.length)
            {
                OnStopped();
            }
        }



        private int GetFrameIndex(GPUSkinningClip clip, float time)
        {
            if (clip.wrapMode == GPUSkinningWrapMode.Once
                && time >= clip.length)
            {
                return clip.frames.Length;
            }
            return (int)(time * clip.fps) % (int)(clip.length * clip.fps);
        }
    }
}
