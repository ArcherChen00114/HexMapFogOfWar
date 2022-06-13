Shader "Custom/Road"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
    }
    SubShader
    {
        Tags {
			"RenderType"="Opaque"
			"Queue" = "Geometry+1"
		}
               //Queue更改绘制队列位置，放置在地形之后
        LOD 200
		Offset -1, -1
        //Offset更改道路渲染的位置，稍微靠近镜头

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows decal:blend vertex:vert
        #pragma multi_compile _ HEX_MAP_EDIT_MODE
        //decal:blend应用一个表面着色器

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
		#include "HexCellData.cginc"

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
			float2 visibility;
        };

        half _Glossiness;
		fixed3 _Specular;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert (inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);

			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);

			
			data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;
		}

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Albedo comes from a texture tinted by color
            float4 noise = tex2D(_MainTex, IN.worldPos.xz * 0.025);
            //缩小坐标比例对噪声贴图进行采样避免整个噪声在道路上平铺
			fixed4 c = _Color * ((noise.y * 0.75 + 0.25) * IN.visibility.x);
			float blend = IN.uv_MainTex.x;//取得uv中的u
			blend *= noise.x + 0.5;
			blend = smoothstep(0.4, 0.7, blend);//在0.4-0.7范围内做平滑曲线，0.4-0.7对应0-1

            float explored = IN.visibility.y;
			o.Albedo = c.rgb;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Alpha = blend * explored;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
