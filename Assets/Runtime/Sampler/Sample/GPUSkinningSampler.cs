using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPUSkinning
{
    public class GPUSkinningSampler
    {
        public bool includeInactive = true;
        private RawDataPerAnimation? rawData = null;

        public bool GenerateRawData(GameObject gameObject)
        {
            if (gameObject == null)
            {
                ShowDialog("GameObject is null");
                return false;
            }

            Animation animation = gameObject.GetComponent<Animation>();
            #region One Renderer
            SkinnedMeshRenderer renderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>(includeInactive);
            if (animation == null || renderer == null)
            {
                ShowDialog("No animation or renderer found on GameObject");
                return false;
            }
            rawData = new RawDataPerAnimation(gameObject.name, animation, renderer);
            #endregion
            #region More Than One Renderer
            //SkinnedMeshRenderer[] renderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);

            //if (animation == null || renderers == null || renderers.Length == 0)
            //{
            //    ShowDialog("No animation or renderers found on GameObject");
            //    return false;
            //}
            //rawData = new RawDataPerAnimation(gameObject.name, animation, renderers);
            #endregion
            return true;
        }

        #region Sample GPUSkinningAnimation
        public GPUSkinningAnimation Sample()
        {
            if (rawData == null)
            {
                Debug.LogError("RawData Needed");
                return null;
            }

            var rawDataPerAni = rawData.Value;
            var renderer = rawDataPerAni.RawDataPerRenderer.Renderer;
            var mesh = renderer.sharedMesh;

            rawDataPerAni.Animation.gameObject.transform.position = Vector3.zero;
            rawDataPerAni.Animation.gameObject.transform.rotation = Quaternion.identity;

            GPUSkinningAnimation samplingAniamtion = ScriptableObject.CreateInstance<GPUSkinningAnimation>();
            samplingAniamtion.name = rawDataPerAni.Name;
            samplingAniamtion.guid = System.Guid.NewGuid().ToString();
            samplingAniamtion.rootBoneIndex = 0;

            List<GPUSkinningBone> bones_result = new List<GPUSkinningBone>();
            CollectBones(bones_result, renderer.bones, mesh.bindposes, null, renderer.rootBone, 0);

            samplingAniamtion.bones = bones_result.ToArray();
            foreach (var animationState in rawDataPerAni.AnimationStates)
            {
                GPUSkinningClip samplingClip = SampleClip(rawData.Value,samplingAniamtion, animationState);
                samplingAniamtion.AddOrReplaceClip(samplingClip);
            }
            CalculateTextureSize(samplingAniamtion);
            return samplingAniamtion;
        }

        private void CollectBones(List<GPUSkinningBone> bones_result, Transform[] bones_smr, Matrix4x4[] bindposes, GPUSkinningBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
        {
            GPUSkinningBone currentBone = new GPUSkinningBone();
            bones_result.Add(currentBone);

            int indexOfSmrBones = System.Array.IndexOf(bones_smr, currentBoneTransform);
            currentBone.transform = currentBoneTransform;
            currentBone.name = currentBone.transform.gameObject.name;
            currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
            currentBone.parentBoneIndex = parentBone == null ? -1 : bones_result.IndexOf(parentBone);

            if (parentBone != null)
            {
                parentBone.childrenBonesIndices[currentBoneIndex] = bones_result.IndexOf(currentBone);
            }

            int numChildren = currentBone.transform.childCount;
            if (numChildren > 0)
            {
                currentBone.childrenBonesIndices = new int[numChildren];
                for (int i = 0; i < numChildren; ++i)
                {
                    CollectBones(bones_result, bones_smr, bindposes, currentBone, currentBone.transform.GetChild(i), i);
                }
            }
        }

        private GPUSkinningClip SampleClip(RawDataPerAnimation rawData, GPUSkinningAnimation samplingAniamtion, AnimationState animationState)
        {
            Debug.Log("Sample Clip Start:" + animationState.clip.name);
            GPUSkinningClip samplingClip = new GPUSkinningClip();
            samplingClip.name = animationState.clip.name;
            samplingClip.fps = (int)animationState.clip.frameRate;
            samplingClip.length = animationState.length;
            samplingClip.wrapMode = (int)animationState.wrapMode <= 1 ? GPUSkinningWrapMode.Once : GPUSkinningWrapMode.Loop;

            int frameCount = (int)(samplingClip.fps * samplingClip.length);
            samplingClip.frames = new GPUSkinningFrame[frameCount];


            Animation animation = rawData.Animation;
            animation.Play(animationState.clip.name);

            float sampleTime = 0f;
            float timePerFrame = animationState.length / frameCount;

            Vector3 rootMotionPosition = Vector3.zero;
            Quaternion rootMotionRotation = Quaternion.identity;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                animationState.time = sampleTime;
                animationState.enabled = true;
                animation.Sample();
                animationState.enabled = false;
                GPUSkinningFrame samplingFrame = SampleFrame(rawData, samplingAniamtion, samplingClip, frameIndex, ref rootMotionPosition, ref rootMotionRotation);
                samplingClip.frames[frameIndex] = samplingFrame;
                sampleTime += timePerFrame;
            }
            return samplingClip;
        }

        private GPUSkinningFrame SampleFrame(RawDataPerAnimation rawData, GPUSkinningAnimation samplingAniamtion, GPUSkinningClip samplingClip, int frameIndex,
                                                    ref Vector3 rootMotionPosition, ref Quaternion rootMotionRotation)
        {
            Debug.Log("Sample Frame Start:" + frameIndex);
            GameObject gameObject = rawData.Animation.gameObject;

            GPUSkinningFrame frame = new GPUSkinningFrame();
            samplingClip.frames[frameIndex] = frame;
            frame.matrices = new Matrix4x4[samplingAniamtion.bones.Length];

            GPUSkinningBone[] bones = samplingAniamtion.bones;
            for (int i = 0; i < bones.Length; ++i)
            {
                GPUSkinningBone currentBone = bones[i];

                frame.matrices[i] = currentBone.bindpose;
                do
                {
                    Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
                    frame.matrices[i] = mat * frame.matrices[i];
                    if (currentBone.parentBoneIndex == -1)
                    {
                        break;
                    }
                    else
                    {
                        currentBone = bones[currentBone.parentBoneIndex];
                    }
                }
                while (true);
            }
            #region RootMotion
            if (frameIndex == 0)
            {
                rootMotionPosition = bones[samplingAniamtion.rootBoneIndex].transform.localPosition;
                rootMotionRotation = bones[samplingAniamtion.rootBoneIndex].transform.localRotation;
            }
            else
            {
                Vector3 newPosition = bones[samplingAniamtion.rootBoneIndex].transform.localPosition;
                Quaternion newRotation = bones[samplingAniamtion.rootBoneIndex].transform.localRotation;
                Vector3 deltaPosition = newPosition - rootMotionPosition;
                //移动方向的Delta
                frame.rootMotionDeltaPositionQ = Quaternion.Inverse(Quaternion.Euler(gameObject.transform.forward.normalized)) * Quaternion.Euler(deltaPosition.normalized);
                frame.rootMotionDeltaPositionL = deltaPosition.magnitude;
                frame.rootMotionDeltaRotation = Quaternion.Inverse(rootMotionRotation) * newRotation;
                rootMotionPosition = newPosition;
                rootMotionRotation = newRotation;

                if (frameIndex == 1)
                {
                    samplingClip.frames[0].rootMotionDeltaPositionQ = samplingClip.frames[1].rootMotionDeltaPositionQ;
                    samplingClip.frames[0].rootMotionDeltaPositionL = samplingClip.frames[1].rootMotionDeltaPositionL;
                    samplingClip.frames[0].rootMotionDeltaRotation = samplingClip.frames[1].rootMotionDeltaRotation;
                }
            }
            #endregion

            return frame;
        }
        #endregion

        #region Create AnimationMap
        private void CalculateTextureSize(GPUSkinningAnimation animation)
        {
            int pixelCount = 0;

            GPUSkinningClip[] clips = animation.clips;
            if (clips == null || clips.Length <= 0)
            {
                Debug.LogError(string.Format("GPUSkinningAnimation {0} has no clips", animation.name));
                return;
            }

            for (int index = 0; index < clips.Length; ++index)
            {
                GPUSkinningClip clip = clips[index];
                clip.pixelSegmentation = pixelCount;

                GPUSkinningFrame[] frames = clip.frames;
                int frameCount = frames.Length;
                pixelCount += animation.bones.Length * 3/* 3 x 4个通道,frame.matrices 中的 3x4 */ * frameCount;
            }

            CalculateTextureSize(pixelCount, out animation.textureWidth, out animation.textureHeight);
        }

        private void CalculateTextureSize(int pixelCount, out int textureWidth, out int textureHeight)
        {
            textureWidth = 1;
            textureHeight = 1;
            while (true)
            {
                if (textureWidth * textureHeight >= pixelCount) break;
                textureWidth *= 2;
                if (textureWidth * textureHeight >= pixelCount) break;
                textureHeight *= 2;
            }
        }

        public Texture2D CreateAnimationMap(GPUSkinningAnimation animation)
        {
            Texture2D texture = new Texture2D(animation.textureWidth, animation.textureHeight, TextureFormat.RGBAHalf, false, true);
            Color[] pixels = texture.GetPixels();
            int pixelIndex = 0;
            for (int clipIndex = 0; clipIndex < animation.clips.Length; ++clipIndex)
            {
                GPUSkinningClip clip = animation.clips[clipIndex];
                GPUSkinningFrame[] frames = clip.frames;
                int numFrames = frames.Length;
                for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
                {
                    GPUSkinningFrame frame = frames[frameIndex];
                    Matrix4x4[] matrices = frame.matrices;
                    int numMatrices = matrices.Length;
                    for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
                    {
                        Matrix4x4 matrix = matrices[matrixIndex];
                        pixels[pixelIndex++] = new Color(matrix.m00, matrix.m01, matrix.m02, matrix.m03);
                        pixels[pixelIndex++] = new Color(matrix.m10, matrix.m11, matrix.m12, matrix.m13);
                        pixels[pixelIndex++] = new Color(matrix.m20, matrix.m21, matrix.m22, matrix.m23);
                    }
                }
            }
            texture.anisoLevel = 0;
            texture.filterMode = FilterMode.Point;
            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
        #endregion

        #region Create GPUSkinningBones
        public Mesh CreateMesh(GPUSkinningAnimation animation)
        {
            SkinnedMeshRenderer renderer = rawData.Value.RawDataPerRenderer.Renderer;
            Mesh mesh = renderer.sharedMesh;

            string meshName = mesh.name;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            Color[] colors = mesh.colors;
            Vector2[] uv = mesh.uv;

            Mesh newMesh = new Mesh();
            newMesh.name = meshName;
            newMesh.vertices = mesh.vertices;
            if (normals != null && normals.Length > 0) { newMesh.normals = normals; }
            if (tangents != null && tangents.Length > 0) { newMesh.tangents = tangents; }
            if (colors != null && colors.Length > 0) { newMesh.colors = colors; }
            if (uv != null && uv.Length > 0) { newMesh.uv = uv; }

            int numVertices = mesh.vertexCount;
            BoneWeight[] boneWeights = mesh.boneWeights;
            Vector4[] uv2 = new Vector4[numVertices];
            Vector4[] uv3 = new Vector4[numVertices];
            Transform[] smrBones = renderer.bones;
            for (int i = 0; i < numVertices; ++i)
            {
                BoneWeight boneWeight = boneWeights[i];

                BoneWeightSortData[] weights = new BoneWeightSortData[4];
                weights[0] = new BoneWeightSortData() { index = boneWeight.boneIndex0, weight = boneWeight.weight0 };
                weights[1] = new BoneWeightSortData() { index = boneWeight.boneIndex1, weight = boneWeight.weight1 };
                weights[2] = new BoneWeightSortData() { index = boneWeight.boneIndex2, weight = boneWeight.weight2 };
                weights[3] = new BoneWeightSortData() { index = boneWeight.boneIndex3, weight = boneWeight.weight3 };
                System.Array.Sort(weights);

                GPUSkinningBone bone0 = GetBoneByTransform(animation,smrBones[weights[0].index]);
                GPUSkinningBone bone1 = GetBoneByTransform(animation,smrBones[weights[1].index]);
                GPUSkinningBone bone2 = GetBoneByTransform(animation, smrBones[weights[2].index]);
                GPUSkinningBone bone3 = GetBoneByTransform(animation, smrBones[weights[3].index]);

                Vector4 skinData_01 = new Vector4();
                skinData_01.x = GetBoneIndex(animation, bone0);
                skinData_01.y = weights[0].weight;
                skinData_01.z = GetBoneIndex(animation, bone1);
                skinData_01.w = weights[1].weight;
                uv2[i] = skinData_01;

                Vector4 skinData_23 = new Vector4();
                skinData_23.x = GetBoneIndex(animation, bone2);
                skinData_23.y = weights[2].weight;
                skinData_23.z = GetBoneIndex(animation, bone3);
                skinData_23.w = weights[3].weight;
                uv3[i] = skinData_23;
            }
            newMesh.SetUVs(1, new List<Vector4>(uv2));
            newMesh.SetUVs(2, new List<Vector4>(uv3));

            newMesh.triangles = mesh.triangles;
            return newMesh;
        }

        private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
        {
            public int index = 0;

            public float weight = 0;

            public int CompareTo(BoneWeightSortData b)
            {
                return weight > b.weight ? -1 : 1;
            }
        }

        private int GetBoneIndex(GPUSkinningAnimation animation, GPUSkinningBone bone)
        {
            return System.Array.IndexOf(animation.bones, bone);
        }

        public static GPUSkinningBone GetBoneByTransform(GPUSkinningAnimation animation, Transform transform)
        {
            GPUSkinningBone[] bones = animation.bones;
            int numBones = bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                if (bones[i].transform == transform)
                {
                    return bones[i];
                }
            }
            return null;
        }
        #endregion

        private static void ShowDialog(string msg)
        {
            UnityEditor.EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
        }
    }

    public static class GPUSkinningExtension 
    {
        public static void AddOrReplaceClip(this GPUSkinningAnimation animation, GPUSkinningClip clip)
        {
            if (animation.clips == null)
                animation.clips = new GPUSkinningClip[] { clip };
            else
            {
                int overrideClipIndex = -1;
                for (int i = 0; i < animation.clips.Length; ++i)
                {
                    if (animation.clips[i].name == animation.name)
                    {
                        overrideClipIndex = i;
                        break;
                    }
                }

                //增加
                if (overrideClipIndex == -1)
                {
                    List<GPUSkinningClip> clips = new List<GPUSkinningClip>(animation.clips);
                    clips.Add(clip);
                    animation.clips = clips.ToArray();
                }
                //替换
                else
                {
                    animation.clips[overrideClipIndex] = clip;
                }
            }
        }
    }
}
