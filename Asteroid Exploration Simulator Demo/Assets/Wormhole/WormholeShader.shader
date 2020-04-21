// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Zhouxun/Universe/Skybox (wormhole)"
{
	Properties
	{
		_SkyboxA ("Space A skybox", CUBE) = "" {}
		_SkyboxB ("Space B skybox", CUBE) = "" {}
//		_Spectrum ("Wormhole Spectrum", 2D) = "" {}
		_CoreSize ("Core Size", Float) = 10
		_CamPos ("Camera Position", Vector) = (100,0,0,0)
	}
	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Background"
		}
		Fog { Mode Off }
		Pass
		{
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			samplerCUBE _SkyboxA;
			samplerCUBE _SkyboxB;
			sampler2D _Spectrum;
			float4 _CamPos;
			float4x4 _CamAngle;
			float _CoreSize;
			struct appdata_skybox
			{
				float4 vertex : POSITION;
			};
			struct v2f 
			{
				float3 wp : TEXCOORD0;
				float4 pos : POSITION0;
			};
			half4 FinalColor (float4 photon)
			{
				return lerp (texCUBE(_SkyboxB, normalize(photon.yzx)),
				texCUBE(_SkyboxA, normalize(photon.xyz)),
				saturate((photon.w + 1) * 0.5));
			}

			#define PI (3.141592654)
//			#define SPEC_SIZE (2048)
//			#define SPEC_PIXEL (0.00048828125)
//			#define SPEC_HALF_PIXEL (0.000244140625)
			float2 OutputAngle2D (float input_angle, float input_space, float dist)
			{
//				float2 tc = 0;
//				tc.x = (input_angle / PI) * (SPEC_SIZE - 1);
//				tc.y = sqrt(max(0,dist-10))*111.1111111;
//
//				float2 frac_tc = float2(frac(tc.x), frac(tc.y));
//
//				float2 tc00 = float2(tc.x, tc.y) * SPEC_PIXEL;
//				float2 tc10 = float2(tc.x + 1, tc.y) * SPEC_PIXEL;
//				float2 tc01 = float2(tc.x, tc.y) * SPEC_PIXEL;
//				float2 tc11 = float2(tc.x + 1, tc.y + 1) * SPEC_PIXEL;
//
//				float3 c00 = tex2D(_Spectrum, tc00).rgb;
//				float output_angle00 = ((round(c00.r * 255) + round(c00.b * 255) * 256) - 32768) * 0.0001;
//				float output_space00 = ((c00.g - 0.25) * 4) * input_space;
//
//				float3 c10 = tex2D(_Spectrum, tc10).rgb;
//				float output_angle10 = ((round(c10.r * 255) + round(c10.b * 255) * 256) - 32768) * 0.0001;
//				float output_space10 = ((c10.g - 0.25) * 4) * input_space;
//
//				float3 c01 = tex2D(_Spectrum, tc01).rgb;
//				float output_angle01 = ((round(c01.r * 255) + round(c01.b * 255) * 256) - 32768) * 0.0001;
//				float output_space01 = ((c01.g - 0.25) * 4) * input_space;
//
//				float3 c11 = tex2D(_Spectrum, tc11).rgb;
//				float output_angle11 = ((round(c11.r * 255) + round(c11.b * 255) * 256) - 32768) * 0.0001;
//				float output_space11 = ((c11.g - 0.25) * 4) * input_space;
//
//				float output_angle = lerp(lerp(output_angle00, output_angle10, frac_tc.x), 
//				lerp(output_angle01, output_angle11, frac_tc.x), frac_tc.y);
//				float output_space = lerp(lerp(output_space00, output_space10, frac_tc.x), 
//				lerp(output_space01, output_space11, frac_tc.x), frac_tc.y);
				
				dist = dist > 0 ? dist + _CoreSize : dist - _CoreSize;
				float t = _CoreSize / dist; // 距离和虫洞内径比值
				float escape_radius = _CoreSize*t*t - 3*_CoreSize*t + 3*_CoreSize; // 等价逃离半径
				float escape_angle = asin(escape_radius/dist); // 逃离角
				// 若光线无偏转，直接进行镜面反射的出射角，作为基准角度output_base
				float output_base = input_angle < escape_angle ? (1-PI/escape_angle)*input_angle + PI : input_angle;
				// 算一个比值
				float u = input_angle < escape_angle ? input_angle/escape_angle : (PI-input_angle)/(PI-escape_angle);
				// 算一个指数
				float power = pow(((input_angle < escape_angle ? 0 : (0.5*PI - escape_angle)/(0.25*PI)) + 1.5),3) * (1+u);
				// 最终比值的指数次方再乘上10倍逃离角即为出射角
				float output_angle = output_base - escape_angle * 10 * pow(u, power);
//
				float delta = escape_angle * 10 * pow(u, power);
				float mix = pow(delta / escape_angle * 0.11f, 2);
				float output_space = input_angle < escape_angle ? input_space : -input_space;
				output_space *= mix - 1;
				return float2(output_angle, output_space);
			}

			v2f vert (appdata_skybox v)
			{
				v2f o;
				o.wp.xyz = v.vertex.xyz;
				o.pos = UnityObjectToClipPos(v.vertex); 
				return o;
			}
			
			half4 frag (v2f i) : COLOR
			{
				float input_space = _CamPos.w;
				float3 dir = normalize(i.wp);
				//dir.x *= input_space;
				float3 elem = -_CamPos.xyz;
				float3 n_elem = normalize(elem);
				float input_angle = acos(clamp(dot(dir, n_elem), -1, 1));
				float dist = length(elem);
				float2 output2d = OutputAngle2D(input_angle, input_space, dist);

				float3 axis = normalize(cross(dir, n_elem));
				float3 curv = normalize(cross(n_elem, axis));
				float _x = cos(output2d.x);
				float _y = sin(output2d.x);
				float3 output_dir = _x * n_elem + _y * curv; 
				output_dir = mul(_CamAngle,float4(output_dir,0)).xyz;
				return pow(FinalColor(float4(output_dir, output2d.y)),1.7);
				//return pow(FinalColor(float4(output_dir, output2d.y))+0.05,3)*12;
			}
			ENDCG 
		}
	}
}