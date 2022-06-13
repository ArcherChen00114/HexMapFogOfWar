Shader "Custom/Terrain" {
	Properties {
	
        //_HeightTex("RGB:Color A:Cutoff",2d)="white"{}
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Terrain Texture Array", 2DArray) = "white" {}
		_GridTex ("Grid Texture", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma multi_compile _ HEX_MAP_EDIT_MODE
		#pragma target 3.5
		
		#pragma multi_compile _ GRID_ON
		#include "HexCellData.cginc"


		UNITY_DECLARE_TEX2DARRAY(_MainTex);
        uniform sampler2D _HeightTex;
		uniform sampler2D _GridTex;

		struct Input {
			float4 color : COLOR;
			float3 worldPos;
			float3 terrain;
			float4 visibility;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void vert (inout appdata_full v, out Input data) {

			UNITY_INITIALIZE_OUTPUT(Input, data);
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);

			data.terrain.x = cell0.w;
			data.terrain.y = cell1.w;
			data.terrain.z = cell2.w;
			//给三个顶点赋值地形类型
			data.visibility.x = cell0.x;
			data.visibility.y = cell1.x;
			data.visibility.z = cell2.x;
			data.visibility.xyz  = lerp(0.25, 1, data.visibility.xyz );
			data.visibility.w =
				cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z;
		}

		float4 GetTerrainColor (Input IN, int index) {
			float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
			float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);
			return c * (IN.color[index] * IN.visibility[index]);

		}


		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = GetTerrainColor(IN, 0) +
				GetTerrainColor(IN, 1) +
				GetTerrainColor(IN, 2);

			fixed4 grid = 1;
			#if defined(GRID_ON)
			float2 gridUV = IN.worldPos.xz;
			gridUV.x *= 1 / (4 * 8.66025404);
			gridUV.y *= 1 / (2 * 15.0);
			grid = tex2D(_GridTex, gridUV);
			#endif
			
			float explored = IN.visibility.w;
			o.Albedo = c.rgb * grid * _Color *explored;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}