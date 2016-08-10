Shader "AltspaceVR/ParticlesAdditiveScrollTexture"
{
	Properties {
		_TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
		_MainTex ("Particle Texture", 2D) = "white" {}
		_InvFade ("Soft Particles Factor", Range(0.01, 3.0)) = 1.0
		_TextureAnimSpeedU ("U Animate Speed", Range(-20.0, 20.0)) = 0.0
		_TextureAnimSpeedV ("V Animate Speed", Range(-20.0, 20.0)) = 0.0
	}

	Category {
		Tags {
				"Queue"="Transparent"
				"IgnoreProjector"="True"
				"RenderType"="Transparent"
			}
		// incoming color is multiple by its alpha; destination (frame buffer)
		// color is multiplied by one during blending
		Blend SrcAlpha One
		// write to RGB
		ColorMask RGB
		Cull Off
		Lighting off
		ZWrite Off

		SubShader {
			Pass {
				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_particles
				#pragma multi_compile_fog

				#include "UnityCG.cginc"

				sampler2D _MainTex;
				fixed4 _TintColor;
				float4 _MainTex_ST;
				float _TextureAnimSpeedU;
				float _TextureAnimSpeedV;

				struct vertexIn
				{
					float4 vertex : POSITION;
					float4 color : COLOR;
					float2 texCoord : TEXCOORD0;
				};

				struct vertexOut
				{
					float4 vertex : SV_POSITION;
					float4 color : COLOR;
					float2 texCoord : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					#ifdef SOFTPARTICLES_ON
					float4 projPos : TEXCOORD2;
					#endif
				};

				vertexOut vert(vertexIn v)
				{
					vertexOut output;
					output.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
					#ifdef SOFTPARTICLES_ON
					output.projPos = ComputeScreenPos(output.vertex);
					COMPUTE_EYEDEPTH(output.projPos.z);
					#endif
					output.color = v.color;
					float2 scrolledTexture = v.texCoord + float2(_Time.y*_TextureAnimSpeedU,
						_Time.y*_TextureAnimSpeedV);
					output.texCoord = TRANSFORM_TEX(scrolledTexture, _MainTex);
					UNITY_TRANSFER_FOG(output, output.vertex);
					return output;
				}

				sampler2D_float _CameraDepthTexture;
				float _InvFade;

				fixed4 frag (vertexOut input) : SV_Target
				{
					#ifdef SOFTPARTICLES_ON
					float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,
									UNITY_PROJ_COORD(input.projPos)));
					float partZ = input.projPos.z;
					// saturate simply clamps between 0 and 1
					float fade = saturate(_InvFade * (sceneZ - partZ));
					input.color.a *= fade;
					#endif
					fixed4 outColor = 2.0f * input.color * _TintColor * 
						tex2D(_MainTex, input.texCoord);
					// fog towards black
					UNITY_APPLY_FOG_COLOR(input.fogCoord, outColor, fixed4(0,0,0,0));
					return outColor;
				}


				ENDCG
			}
		}
	}

}