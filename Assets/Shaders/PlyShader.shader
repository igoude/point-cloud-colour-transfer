Shader "Unlit/PlyShader"
{
	Properties
	{
		_Size("Size", Range(0, 3)) = 1.0
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque"}

		// Point pass
		Pass
		{
			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float3 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2g {
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 color : COLOR0;
			};

			struct g2f {
				float4 vertex : SV_POSITION;
				float3 color : COLOR0;
			};

			float _Size;

			int _PositionSpace;		// 0 : position, 1 : color, 2 : normal...
			
			float _SwitchSpace;
			float _SwitchNormal;

			float3 _Translation;

			float4x4 _EigenNormals;
			int _NormalsDebuger;

			v2g vert(appdata v)
			{
				v2g o;
				
				// Normal 
				float3 eigenNormal = normalize(mul(_EigenNormals, float4(v.normal, 0.0))).xyz;
				eigenNormal = (_SwitchNormal*eigenNormal) + ((1.0 - _SwitchNormal)*v.normal);

				float3 color = v.color;

				// Position transformer
				float4 position;
				if (_PositionSpace == 0) {
					float4 positionPos = UnityObjectToClipPos(v.vertex);
					
					// RGB color space
					float4 colorPos = mul(UNITY_MATRIX_VP, float4(color + _Translation, 1.0));
					
					position = _SwitchSpace * colorPos + (1.0 - _SwitchSpace) * positionPos;
				}
				else if (_PositionSpace == 1) {
					float4 positionPos = UnityObjectToClipPos(v.vertex);
					
					// Normal color space
					float scale = length(color);
					float4 normalPos = mul(UNITY_MATRIX_VP, float4(eigenNormal*scale + _Translation, 1.0));

					position = _SwitchSpace * normalPos + (1.0 - _SwitchSpace) * positionPos;
				}
				else {
					position = UnityObjectToClipPos(v.vertex);
				}


				// Normal transformer
				float3 normal = normalize(mul(UNITY_MATRIX_V, float4(v.normal, 0.0)).xyz);
				
				o.vertex = position;
				o.normal = normal;
				o.color = color;

				// Debuger
				if (_NormalsDebuger == 1) {
					o.color = float3(eigenNormal.x, eigenNormal.y, -eigenNormal.z);;
				}

				return o;
			}

			[maxvertexcount(4)]
			void geom(point v2g input[1], inout TriangleStream<g2f> triStream) {
				float4 position = input[0].vertex;
				float3 normal = input[0].normal;
				float3 color = input[0].color;

				float ratio = float(_ScreenParams.y) / float(_ScreenParams.x);

				float4 x = -float4(1.0, 0.0, 0.0, 0.0) * _Size * ratio;
				float4 y = -float4(0.0, 1.0, 0.0, 0.0) * _Size;
					
				g2f o;
				o.color = color;

				o.vertex = position - 0.5*x - 0.5*y;
				triStream.Append(o);

				o.vertex = position + 0.5*x - 0.5*y;
				triStream.Append(o);

				o.vertex = position - 0.5*x + 0.5*y;
				triStream.Append(o);

				o.vertex = position + 0.5*x + 0.5*y;
				triStream.Append(o);
			}

			fixed4 frag(g2f i) : SV_Target
			{
				fixed4 color = float4(i.color, 1.0);
				return color;
			}
			ENDCG
		}
	}
}
