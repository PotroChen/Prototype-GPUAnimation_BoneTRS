// Upgrade NOTE: upgraded instancing buffer 'GPUSkinningProperties0' to new syntax.
// Upgrade NOTE: upgraded instancing buffer 'GPUSkinningProperties1' to new syntax.
// Upgrade NOTE: upgraded instancing buffer 'GPUSkinningProperties2' to new syntax.

#ifndef GPUSKINNING_TEST_INCLUDE
#define GPUSKINNING_TEST_INCLUDE

uniform sampler2D _AnimationMap;
uniform float3 _AnimationMapInfo;

UNITY_INSTANCING_BUFFER_START(GPUSkinningProperties0)
	UNITY_DEFINE_INSTANCED_PROP(float2, _PixelSegmentation)
UNITY_INSTANCING_BUFFER_END(GPUSkinningProperties0)

inline float4 indexToUV(float index)
{
	int row = (int)(index / _AnimationMapInfo.x);
	float col = index - row * _AnimationMapInfo.x;
	return float4(col / _AnimationMapInfo.x, row / _AnimationMapInfo.y, 0, 0);
}

inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
{
	float matStartIndex = frameStartIndex + boneIndex * 3;
	float4 row0 = tex2Dlod(_AnimationMap, indexToUV(matStartIndex));
	float4 row1 = tex2Dlod(_AnimationMap, indexToUV(matStartIndex + 1));
	float4 row2 = tex2Dlod(_AnimationMap, indexToUV(matStartIndex + 2));
	float4 row3 = float4(0, 0, 0, 1);
	float4x4 mat = float4x4(row0, row1, row2, row3);
	return mat;
}

inline float getFrameStartIndex()
{
	float2 frameIndex_segment = UNITY_ACCESS_INSTANCED_PROP(_PixelSegmentation_arr, _PixelSegmentation);
	float segment = frameIndex_segment.y;
	float frameIndex = frameIndex_segment.x;
	float frameStartIndex = segment + frameIndex * _AnimationMapInfo.z;
	return frameStartIndex;
}

#define textureMatrix(uv2, uv3) float frameStartIndex = getFrameStartIndex(); \
								float4x4 mat0 = getMatrix(frameStartIndex, uv2.x); \
								float4x4 mat1 = getMatrix(frameStartIndex, uv2.z); \
								float4x4 mat2 = getMatrix(frameStartIndex, uv3.x); \
								float4x4 mat3 = getMatrix(frameStartIndex, uv3.z);

#define skin4_noroot(mat0, mat1, mat2, mat3) mul(mat0, vertex) * uv2.y + \
												mul(mat1, vertex) * uv2.w + \
												mul(mat2, vertex) * uv3.y + \
												mul(mat3, vertex) * uv3.w;

#define rootOff_BlendOff(quality) textureMatrix(uv2, uv3); \
									return skin4_noroot(mat0, mat1, mat2, mat3);

inline float4 skin4(float4 vertex, float4 uv2, float4 uv3)
{
	rootOff_BlendOff(4);

	return 0;
}

#endif