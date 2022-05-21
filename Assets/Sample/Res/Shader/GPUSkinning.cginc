#ifndef GPUSKINNING_INCLUDE
#define GPUSKINNING_INCLUDE

sampler2D _AnimationMap;
float3 _AnimationMapInfo;//x:textureWidth,y:textureHeight,z:PixelCountPerFrame(BoneCount * float3x4)

UNITY_INSTANCING_BUFFER_START(GPUSKinningPerMaterial)
	UNITY_DEFINE_INSTANCED_PROP(float2, _PixelSegmentation)
UNITY_INSTANCING_BUFFER_END(GPUSKinningPerMaterial)

#define GPUSKINING_INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(GPUSKinningPerMaterial, name)

float4 IndexToUV(float index)
{
	float textureWidth = _AnimationMapInfo.x;
	float textureHeight = _AnimationMapInfo.y;

	int row = (int)(index / textureWidth);
	float col = index - row * textureWidth;
	return float4(col / textureWidth, row / textureHeight,0,0);
}

float4x4 GetBoneTRSMatrix_OS(int frameStartIndex, float boneIndex)
{
	float matStartIndex = frameStartIndex + boneIndex * 3;
	float4 row0 = tex2Dlod(_AnimationMap, IndexToUV(matStartIndex));
	float4 row1 = tex2Dlod(_AnimationMap, IndexToUV(matStartIndex + 1));
	float4 row2 = tex2Dlod(_AnimationMap, IndexToUV(matStartIndex + 2));
	float4 row3 = float4(0, 0, 0, 1);
	float4x4 mat = float4x4(row0, row1, row2, row3);
	return mat;
}

float GetFrameStartIndex()
{
	float2 frameIndex_segment = GPUSKINING_INPUT_PROP(_PixelSegmentation);
	float segment = frameIndex_segment.y;
	float frameIndex = frameIndex_segment.x;
	float frameStartIndex = segment + frameIndex * _AnimationMapInfo.z;
	return frameStartIndex;
}

float4 GetProcessedPositionOSFromAnimationMap(float4 positionOS,float4 uv2,float4 uv3)
{
	float frameStartIndex = GetFrameStartIndex();

	float4x4 bone1TRS = GetBoneTRSMatrix_OS(frameStartIndex,uv2.x);
	float bone1Weight = uv2.y;

	float4x4 bone2TRS = GetBoneTRSMatrix_OS(frameStartIndex,uv2.z);
	float bone2Weight = uv2.w;

	float4x4 bone3TRS = GetBoneTRSMatrix_OS(frameStartIndex,uv3.x);
	float bone3Weight = uv3.y;

	float4x4 bone4TRS = GetBoneTRSMatrix_OS(frameStartIndex,uv3.z);
	float bone4Weight = uv3.w;

	float4 processedPositionOS = mul(bone1TRS, positionOS) * bone1Weight + mul(bone2TRS, positionOS) * bone2Weight + mul(bone3TRS, positionOS) * bone3Weight + mul(bone4TRS, positionOS) * bone4Weight;

	return processedPositionOS;
}



#endif