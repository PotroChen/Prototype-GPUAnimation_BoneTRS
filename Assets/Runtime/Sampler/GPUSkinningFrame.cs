using UnityEngine;

namespace GPUSkinning
{
    [System.Serializable]
    public class GPUSkinningFrame
    {
        /// <summary>
        /// 当前帧，骨骼的模型空间坐标,旋转，缩放
        /// 最终会存在贴图里面,Runtime用不到（除非使用了Joint?）
        /// </summary>
        public Matrix4x4[] matrices = null;

        #region RootMotion
        public Quaternion rootMotionDeltaPositionQ;

        public float rootMotionDeltaPositionL;

        public Quaternion rootMotionDeltaRotation;
        #endregion
    }
}