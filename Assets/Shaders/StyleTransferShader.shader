Shader "Unlit/StyleTransferShader"
{
	SubShader
	{
		Tags { "RenderType" = "Opaque"}

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


			float3 RGBtoLab(float3 RGB) {
				// RGB to LMS
				float L = RGB.r * 0.3811 + RGB.g * 0.5783 + RGB.b * 0.0402;
				float M = RGB.r * 0.1967 + RGB.g * 0.7244 + RGB.b * 0.0782;
				float S = RGB.r * 0.0241 + RGB.g * 0.1288 + RGB.b * 0.8444;

				L = log10(L + 0.000001);
				M = log10(M + 0.000001);
				S = log10(S + 0.000001);

				// LMS to Lab
				float l = L + M + S;
				float a = L + M - 2.0 * S;
				float b = L - M;

				l = (1.0 / sqrt(3.0)) * l;
				a = (1.0 / sqrt(6.0)) * a;
				b = (1.0 / sqrt(2.0)) * b;

				return float3(l, a, b);
			}

			float3 LabtoRGB(float3 Lab) {
				// Lab to LMS
				float l = (sqrt(3.0) / 3.0) * Lab.r;
				float a = (sqrt(6.0) / 6.0) * Lab.g;
				float b = (sqrt(2.0) / 2.0) * Lab.b;

				float L = l + a + b;
				float M = l + a - b;
				float S = l - 2.0 * a;

				L = pow(10.0, L);
				M = pow(10.0, M);
				S = pow(10.0, S);

				// LMS to RGB
				float R = L * 4.4678 - M * 3.5873 + S * 0.1193;
				float G = -L * 1.2186 + M * 2.3809 - S * 0.1624;
				float B = L * 0.0497 - M * 0.2439 + S * 1.2045;

				// Clamp
				R = clamp(R, 0.0, 1.0);
				G = clamp(G, 0.0, 1.0);
				B = clamp(B, 0.0, 1.0);

				return float3(R, G, B);
			}

			

			// Points attributes
			float _Size;
			float _Exposure;


			// Reinhard 
			float _InputMeans[9];
			float _InputStds[9];
			float _TargetMeans[9];
			float _TargetStds[9];

			float3 Reinhard(float3 color) {
				float3 RGB = color;
				float3 Lab = RGBtoLab(RGB);

				float3 inputMeans = float3(_InputMeans[0], _InputMeans[1], _InputMeans[2]);
				float3 inputStds = float3(_InputStds[0], _InputStds[1], _InputStds[2]);

				float3 targetMeans = float3(_TargetMeans[0], _TargetMeans[1], _TargetMeans[2]);
				float3 targetStds = float3(_TargetStds[0], _TargetStds[1], _TargetStds[2]);

				// Substract source mean
				Lab -= inputMeans;
				// Correct deviations (target / input)
				Lab *= (targetStds / inputStds);
				// Add target mean
				Lab += targetMeans;

				float3 transfer = LabtoRGB(Lab);

				return transfer * _Exposure;
			}


			// Reinhard Normal
			float _InputMeansL[6];
			float _InputMeansA[6];
			float _InputMeansB[6];
			float _InputStdsL[6];
			float _InputStdsA[6];
			float _InputStdsB[6];

			float _TargetMeansL[6];
			float _TargetMeansA[6];
			float _TargetMeansB[6];
			float _TargetStdsL[6];
			float _TargetStdsA[6];
			float _TargetStdsB[6];

			float3 ReinhardNormal(float3 color, float3 normal) {
				float3 RGB = color;
				float3 Lab = RGBtoLab(RGB);
				
				int x_side;
				if (normal.x < 0.0) { x_side = 0; }
				else { x_side = 1; }

				int y_side;
				if (normal.y < 0.0) { y_side = 2; }
				else { y_side = 3; }

				int z_side;
				if (normal.z < 0.0) { z_side = 4; }
				else { z_side = 5; }

				float norm = abs(normal.x) + abs(normal.y) + abs(normal.z);
				float nx = abs(normal.x) / norm;
				float ny = abs(normal.y) / norm;
				float nz = abs(normal.z) / norm;

				float3 inputMeansX = float3(_InputMeansL[x_side], _InputMeansA[x_side], _InputMeansB[x_side]);
				float3 inputMeansY = float3(_InputMeansL[y_side], _InputMeansA[y_side], _InputMeansB[y_side]);
				float3 inputMeansZ = float3(_InputMeansL[z_side], _InputMeansA[z_side], _InputMeansB[z_side]);
				float3 inputMeans = nx * inputMeansX + ny * inputMeansY + nz * inputMeansZ;

				float3 inputStdsX = float3(_InputStdsL[x_side], _InputStdsA[x_side], _InputStdsB[x_side]);
				float3 inputStdsY = float3(_InputStdsL[y_side], _InputStdsA[y_side], _InputStdsB[y_side]);
				float3 inputStdsZ = float3(_InputStdsL[z_side], _InputStdsA[z_side], _InputStdsB[z_side]);
				float3 inputStds = nx * inputStdsX + ny * inputStdsY + nz * inputStdsZ;

				float3 targetMeansX = float3(_TargetMeansL[x_side], _TargetMeansA[x_side], _TargetMeansB[x_side]);
				float3 targetMeansY = float3(_TargetMeansL[y_side], _TargetMeansA[y_side], _TargetMeansB[y_side]);
				float3 targetMeansZ = float3(_TargetMeansL[z_side], _TargetMeansA[z_side], _TargetMeansB[z_side]);
				float3 targetMeans = nx * targetMeansX + ny * targetMeansY + nz * targetMeansZ;

				float3 targetStdsX = float3(_TargetStdsL[x_side], _TargetStdsA[x_side], _TargetStdsB[x_side]);
				float3 targetStdsY = float3(_TargetStdsL[y_side], _TargetStdsA[y_side], _TargetStdsB[y_side]);
				float3 targetStdsZ = float3(_TargetStdsL[z_side], _TargetStdsA[z_side], _TargetStdsB[z_side]);
				float3 targetStds = nx * targetStdsX + ny * targetStdsY + nz * targetStdsZ;


				// Substract source mean
				Lab -= inputMeans;
				// Correct standard deviation (target / input)
				Lab *= (targetStds / inputStds);
				// Add target mean
				Lab += targetMeans;

				float3 transfer = LabtoRGB(Lab);
				
				return transfer * _Exposure;
			}

			
			// Pitié
			float _TransformL[3];
			float _TransformA[3];
			float _TransformB[3];

			float3 Pitie(float3 color) {
				float3 RGB = color;
				float3 Lab = RGBtoLab(RGB);

				float input[3] = { Lab.r, Lab.g, Lab.b };

				float3 transfer = float3(0.0, 0.0, 0.0);

				int i = 0;
				for (i = 0; i < 3; i++) {
					input[i] -= _InputMeans[i];
				}
				for (i = 0; i < 3; i++) {
					transfer.r += _TransformL[i] * input[i];
					transfer.g += _TransformA[i] * input[i];
					transfer.b += _TransformB[i] * input[i];
				}
				transfer.r += _TargetMeans[0];
				transfer.g += _TargetMeans[1];
				transfer.b += _TargetMeans[2];

				transfer = LabtoRGB(transfer);

				return transfer * _Exposure;
			}


			// Pitié with normals
			float _TransformLNormal[9];
			float _TransformANormal[9];
			float _TransformBNormal[9];

			float3 PitieNormal(float3 color, float3 normal) {
				float3 RGB = color;
				float3 Lab = RGBtoLab(RGB);

				float input[9] = { Lab.r, Lab.g, Lab.b, abs(min(normal.x, 0.0)), max(normal.x, 0.0), abs(min(normal.y, 0.0)), max(normal.y, 0.0) , abs(min(normal.z, 0.0)), max(normal.z, 0.0) };
				int dim = 9;
				
				float3 transfer = float3(0.0, 0.0, 0.0);

				int i = 0;
				for (i = 0; i < dim; i++) {
					input[i] -= _InputMeans[i];
				}

				for (i = 0; i < dim; i++) {
					transfer.r += _TransformLNormal[i] * input[i];
					transfer.g += _TransformANormal[i] * input[i];
					transfer.b += _TransformBNormal[i] * input[i];
				}
	
				transfer.r += _TargetMeans[0];
				transfer.g += _TargetMeans[1];
				transfer.b += _TargetMeans[2];

				transfer = LabtoRGB(transfer);

				return transfer * _Exposure;
			}
			

			int _PositionSpace;		// 0 : position, 1 : color, 2 : normal...
			int _TransferMethod;	// 0 : Reinhard, 1 : ReinhardNormal, 2 : Pitié, 3 : PitiéNormal

			float _SwitchSpace;
			float _SwitchTransfer;
			float _SwitchNormal;

			float3 _Translation;
			float3 _MinPosition;
			float3 _MaxPosition;

			float4x4 _EigenNormals;

			int _NormalsDebuger;

			v2g vert(appdata v)
			{
				v2g o;

				// Normal 
				float3 rotNormal = normalize(mul(UNITY_MATRIX_M, float4(v.normal, 0.0))).xyz;
				float3 eigenNormal = normalize(mul(_EigenNormals, float4(v.normal, 0.0))).xyz;
				float3 rotEigNormal = normalize(mul(UNITY_MATRIX_M, float4(eigenNormal, 0.0))).xyz;
				eigenNormal = (_SwitchNormal*rotEigNormal) + ((1.0 - _SwitchNormal)*rotNormal);

				// Position
				float3 eigenPosition = (v.vertex.xyz - _MinPosition) / (_MaxPosition - _MinPosition);

				// Color transfer
				float3 transfer = float3(0.0, 0.0, 0.0);
				if (_TransferMethod == 0) {			// Reinhard
					transfer = Reinhard(v.color);
				}
				else if (_TransferMethod == 1) {	// Reinhard Normal
					transfer = ReinhardNormal(v.color, eigenNormal);
				}
				else if (_TransferMethod == 2) {	// Pitié
					transfer = Pitie(v.color);
				}
				else if (_TransferMethod == 3) {	// Pitié Normal
					transfer = PitieNormal(v.color, eigenNormal);
				}
				else {
					transfer = v.color;
				}
				float3 color = (_SwitchTransfer*transfer) + ((1.0 - _SwitchTransfer)*v.color);
				

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

				if (_NormalsDebuger == 1) {
					o.color = float3(eigenNormal.x, eigenNormal.y, -eigenNormal.z);
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
