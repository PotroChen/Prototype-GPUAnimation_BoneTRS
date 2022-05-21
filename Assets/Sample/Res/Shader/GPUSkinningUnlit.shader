Shader "GPUSkinning/Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AnimationMap("AnimationMap",2D) = "white"{}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            //#include "GPUSkinningOri.cginc"
            #include "GPUSkinning.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;//x:bone1_Index,y:bone1_weight,z:bone2_Index,w:bone2_weight
		        float4 uv3 : TEXCOORD2;//x:bone3_Index,y:bone3_weight,z:bone4_Index,w:bone4_weight
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;


            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;

                float4 positionOS = GetProcessedPositionOSFromAnimationMap(v.vertex,v.uv2,v.uv3);

                o.vertex = UnityObjectToClipPos(positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
