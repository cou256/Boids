
Shader "Instanced/DrawMeshInstanced" {
	Properties {
		[HDR] _Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert addshadow
		#pragma multi_compile_instancing
		#pragma instancing_options procedural:setup
		#pragma target 5.0

		#include "../Cginc/Transform.cginc"
		#include "../Cginc/Color.cginc"

		struct TransformStruct{
			float3 translate;
			float3 rotation;
			float3 velocity;
			float3 center;
			uint centerCount;
			float3 separate;
			uint separateCount;
			float3 velocitySum;
		    uint velocitySumCount;
		};

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		StructuredBuffer<TransformStruct> _TransformBuff;
		#endif

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			fixed4 color;
		};

		half _Glossiness;
		half _Metallic;
		float4 _Color;
		float3 _Scale;

		void setup()
		{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			TransformStruct t = _TransformBuff[unity_InstanceID];
			unity_ObjectToWorld = mul(translate_m(t.translate), mul(rotate_m(t.rotation), scale_m(_Scale)));
			#endif
		}
		void vert(inout appdata_full v) {
			v.normal = normalize(mul(unity_ObjectToWorld, v.normal));
		}
		void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Albedo = _Color;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
