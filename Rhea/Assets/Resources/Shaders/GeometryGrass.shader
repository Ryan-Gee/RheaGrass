Shader "Custom/GeometryGrass" 
{
	Properties{
		//The color at the root of the grass blade
		_BaseColor("Base Color", Color) = (1, 1, 1, 1)
		//Color at the end of the grass blade
		_FadeColor("Fade Color", Color) = (0, 0, 0, 1)
		//Base pixel texture applied to blades (including alpha if desired)
		_BaseMap("Blade Texture", 2D) = "white" {}

		//Brightness variation per-blade
		_LumaVariation("Luma Variation", Range(0, 1)) = 0.5
		//Brightness variation per-blade
		_HeightLuma("Height Brightness", Range(0, 1)) = 0.5
		//Red and green variation per-blade
		_HueVariation("Hue Variation", Range(0, 1)) = 0.5

		//Do the blades have a triangle end (as opposed to a flat square card)
		[Toggle(TAPERED_BLADES)] _Taper("Tapered Blades", Float) = 0
		//The number of textures arrange horizontally on the supplied blade texture
		_AtlasLength("Texture Atlas Length", Range(1, 6)) = 1
		//Bias towards the left side of the texture (will more frequently use textures from the left side than the right)
		_AtlasBias("Texture Atlas Bias", Range(1, 8)) = 2
		//Weights the atlas so textures on the left occur on shorter blades, right-hand textures occurs on longer blades
		_AtlasHeightBias("Height Atlas Bias", Range(0, 1)) = 0.5

		//Minimum blade width
		_BladeWidthMin("Blade Width Min", Range(0, 0.1)) = 0.03
		//Maximum blade width
		_BladeWidthMax("Blade Width Max", Range(0, 0.5)) = 0.1
		//Minimum blade height
		_BladeHeightMin("Blade Height Min", Range(0, 2)) = 0.15
		//Maximum blade height
		_BladeHeightMax("Blade Height Max", Range(0, 2)) = 0.6

		//Blade tip bending
		_BladeBendDistance("Blade Bend Distance", Range(-0.5, 0.5)) = -0.1
		//Depth of blade curvature
		_BladeBendCurve("Blade Curvature", Range(1, 4)) = 2

		//Position randomization per-blade
		_BladeJitter("Position Jitter", Range(0, 0.5)) = 0.01

		//Angle randomization per-blade
		_BendDelta("Bend Variation", Range(0, 1)) = 0

		//Overall tessellation depth
		_TessellationGrassDist("Tessellation Grass Distance", Range(0.1, 2)) = 0.9
		//Tessellation view distance minimum weighting
		_TessellationViewMin("Tessellation Minimum View Distance", Range(1, 100)) = 20
		//Tessellation view distance maximum weighting
		_TessellationViewMax("Tessellation Maximum View Distance", Range(1, 100)) = 100

		//Grass trimming pixel map
		_GrassMap("Grass Map", 2D) = "white" {}
		//Minimum cutoff level of grass map for spawning blades
		_GrassThreshold("Grass Threshold", Range(-0.1, 1)) = 0
		//Power falloff of grass map
		_GrassFalloff("Grass Falloff", Range(0, 1)) = 0.2

		//Map used for wind distortion
		_WindMap("Wind Offset Map", 2D) = "bump" {}
		//Strength and direction of the wind distortion
		_WindVelocity("Wind Velocity", Vector) = (1, 0.1, 0, 0)
		//Panning speed of the wind texture
		_WindFrequency("Wind Frequency", Range(0, 0.2)) = 0.01

		//Object displacement position (set via script)
		[HideInInspector]
		//_DisplacementCenter("Displacement Position", Vector) = (0, 0, 0, 0)
		//Radius of circular displacement around displacementCenter
		_DisplacementRadius("Displacement Radius", Range(0.1, 3)) = 1
		//Strength of radial displacement
		_DisplacementStrength("Displacement Strength", Range(0, 5)) = 1.5
		//Radial displacement falloff
		_DisplacementFalloff("Displacement Falloff", Range(0.1, 2)) = 0.6

		//Multiplier of the shadow bias
		_ShadowBiasStrength("Shadow Bias Strength", Range(0, 0.2)) = 0.07
		//Alpha clipping
		_Cutoff("Cutoff", Range(0, 1)) = 0.5
	}

	SubShader{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

		HLSLINCLUDE

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		CBUFFER_START(UnityPerMaterial)
		float4 _BaseColor;
		float4 _FadeColor;
		float4 _BaseMap_ST;

		float _LumaVariation;
		float _HeightLuma;
		float _HueVariation;

		float _BladeWidthMin;
		float _BladeWidthMax;
		float _BladeHeightMin;
		float _BladeHeightMax;

		float _BladeBendDistance;
		float _BladeBendCurve;

		float _BladeJitter;
		float _BendDelta;
		float _TessellationGrassDist;
		float _TessellationViewMin;
		float _TessellationViewMax;

		sampler2D _GrassMap;
		float4 _GrassMap_ST;
		float _GrassThreshold;
		float _GrassFalloff;

		sampler2D _WindMap;
		float4 _WindMap_ST;
		float4 _WindVelocity;
		float _WindFrequency;
		float _ShadowColor;

		float _DisplacementRadius;
		float _DisplacementStrength;
		float _DisplacementFalloff;

		float _AtlasLength;
		float _AtlasBias;
		float _AtlasHeightBias;

		float _ShadowBiasStrength;
		float _Cutoff;

		CBUFFER_END

		//Grab light direction for use in shadow bias calculations later
		float3 _LightDirection;
		float4 _LightColor0;
		float4 _DisplacementCenter;

		//////////////////////////////////////////////////
		//					DEFINES						//
		//////////////////////////////////////////////////


		#define UNITY_PI 3.14159265359f
		#define UNITY_TWO_PI 6.28318530718f
		//Change this to change the number of subdividisions per grass blade
		#define BLADE_SEGMENTS 4
		//Shader feature for blade tapering
		#pragma shader_feature TAPERED_BLADES
			


		//////////////////////////////////////////////////
		//					STRUCTS						//
		//////////////////////////////////////////////////

		struct VertexInput
		{
			float4 vertex	: POSITION;
			float3 normal	: NORMAL;
			float4 tangent	: TANGENT;
			float2 uv		: TEXCOORD0;
			float4 color	: COLOR;
		};

		struct VertexOutput
		{
			float4 vertex	: SV_POSITION;
			float3 normal	: NORMAL;
			float4 tangent	: TANGENT;
			float2 uv		: TEXCOORD0;
			float4 color	: COLOR;
		};

		struct GeometryData
		{
			float4 pos	: SV_POSITION;
			float2 uv	: TEXCOORD0;
			float3 worldPos	: TEXCOORD1;
			float4 color	: COLOR;
		};

		struct TessellationFactors
		{
			float edge[3] : SV_TessFactor;
			float inside : SV_InsideTessFactor;
		};

		//Texture declares
		TEXTURE2D(_BaseMap);
		SAMPLER(sampler_BaseMap);
		SAMPLER(sampler_WindMap);


		//////////////////////////////////////////////////
		//					FUNCTIONS					//
		//////////////////////////////////////////////////

		GeometryData TransformGeometryToClip(float3 pos, float3 offset, float3x3 transformationMatrix, float2 uv, float4 color)
		{
			GeometryData o;
			float3 offsetPos = pos + mul(transformationMatrix, offset);
			o.pos = TransformObjectToHClip(offsetPos);

			//If this is being used in the shadow pass, apply shadow biasing
#ifdef UNITY_PASS_SHADOWCASTER	
			float3 lightDirectionWS = _LightDirection;
			//Use Unity's ApplyShadowBias function scaled by a custom bias strength value
			//A little hacky as the supplied normal isn't true OS normals, but it works 
			o.pos.xyz = ApplyShadowBias((o.pos), float3(0, 0, _ShadowBiasStrength), lightDirectionWS * 0.01).xyz;
#endif

			o.uv = uv;
			o.worldPos = TransformObjectToWorld(offsetPos);
			o.color = color;

			return o;
		}

		//Return a random number based on a vector seed
		float rand(float3 co)
		{
			return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
		}

		//Angle axis function courtesy of Keijiro
		//https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
		float3x3 angleAxis3x3(float angle, float3 axis)
		{
			float c, s;
			sincos(angle, s, c);

			float t = 1 - c;
			float x = axis.x;
			float y = axis.y;
			float z = axis.z;

			return float3x3
				(
					t * x * x + c, t * x * y - s * z, t * x * z + s * y,
					t * x * y + s * z, t * y * y + c, t * y * z - s * x,
					t * x * z - s * y, t * y * z + s * x, t * z * z + c
					);
		}

		//General tessellation boilerplate for calculating edges based on view distance
		float tessellationEdgeFactor(VertexInput vert0, VertexInput vert1)
		{
			float3 v0 = vert0.vertex.xyz;
			float3 v1 = vert1.vertex.xyz;
			float edgeLength = distance(v0, v1);

			float3 edgeCenter = (v0 + v1) * 0.5f;
			float viewDist = clamp(distance(edgeCenter, _WorldSpaceCameraPos), _TessellationViewMin, _TessellationViewMax) * (_TessellationGrassDist / 100);

			return edgeLength / viewDist;
		}

		//Boilerplate tessellation patch function
		TessellationFactors patchConstantFunc(InputPatch<VertexInput, 3> patch)
		{
			TessellationFactors f;

			f.edge[0] = tessellationEdgeFactor(patch[1], patch[2]);
			f.edge[1] = tessellationEdgeFactor(patch[2], patch[0]);
			f.edge[2] = tessellationEdgeFactor(patch[0], patch[1]);
			f.inside = (f.edge[0] + f.edge[1] + f.edge[2]) / 3.0f;

			return f;
		}



		//////////////////////////////////////////////////
		//				MAIN FUNCTIONS					//
		//////////////////////////////////////////////////

		VertexOutput vert(VertexInput v)
		{
			VertexOutput o;
			o.vertex = float4(TransformObjectToWorld(v.vertex), 1.0f);
			o.normal = TransformObjectToWorldNormal(v.normal);
			o.tangent = v.tangent;
			o.uv = TRANSFORM_TEX(v.uv, _GrassMap);
			o.color = v.color;
			return o;
		}

		//Tesselation hull function
		[domain("tri")]
		[outputcontrolpoints(3)]
		[outputtopology("triangle_cw")]
		[partitioning("integer")]
		[patchconstantfunc("patchConstantFunc")]
		VertexInput hull(InputPatch<VertexInput, 3> patch, uint id : SV_OutputControlPointID)
		{
			return patch[id];
		}

		//Tesselation vertex function
		VertexOutput tessVert(VertexInput v)
		{
			VertexOutput o;
			o.vertex = v.vertex;
			o.normal = v.normal;
			o.tangent = v.tangent;
			o.uv = v.uv;
			o.color = v.color;
			return o;
		}

		//Tessellation domain function
		[domain("tri")]
		VertexOutput domain(TessellationFactors factors, OutputPatch<VertexInput, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
		{
			VertexInput i;

			#define INTERPOLATE(fieldname) i.fieldname = \
			patch[0].fieldname * barycentricCoordinates.x + \
			patch[1].fieldname * barycentricCoordinates.y + \
			patch[2].fieldname * barycentricCoordinates.z;

			INTERPOLATE(vertex);
			INTERPOLATE(normal);
			INTERPOLATE(tangent);
			INTERPOLATE(uv);
			INTERPOLATE(color);

			return tessVert(i);
		}

		//Need to change the geometry vertex count if the end tapers to a single vertex or ends with two as a card
#ifdef TAPERED_BLADES
		[maxvertexcount(BLADE_SEGMENTS * 2 + 1)]
#else
		[maxvertexcount((BLADE_SEGMENTS + 1) * 2)]
#endif
		void geom(point VertexOutput input[1], inout TriangleStream<GeometryData> triStream)
		{
			//Sample the trimming map first to determine if grass will be spawned at this vertex at all
			float trimming = tex2Dlod(_GrassMap, float4(input[0].uv, 0, 0)).r;
			if (trimming >= _GrassThreshold)
			{
				//Grass map trimming falloff
				float falloff = smoothstep(_GrassThreshold, _GrassThreshold + _GrassFalloff, trimming);

				//Initialize positional and directional data
				float3 pos = input[0].vertex.xyz;
				float3 normal = input[0].normal;
				float3 tangent = input[0].tangent;
				float3 bitangent = cross(normal, tangent);
				float4 tint = input[0].color;

				//Jitter position based off tangent and bitangent for non-flat objects
				pos += ((rand(pos.xyz) * 2) - 1) * tangent * _BladeJitter;
				pos += ((rand(pos.zyx) * 2) - 1) * bitangent * _BladeJitter;

				//Tangent to local conversion matrix
				float3x3 tangentToLocal = float3x3
					(
						tangent.x, bitangent.x, normal.x,
						tangent.y, bitangent.y, normal.y,
						tangent.z, bitangent.z, normal.z
						);

				//Calculate wind factor
				//Scale, offset, and panning for wind uv
				float2 windUV = pos.xz * _WindMap_ST.xy + _WindMap_ST.zw + normalize(_WindVelocity.xzy) * _WindFrequency * _Time.y;
				//Sample the wind map for the displacement vector at this location
				float2 windSample = (tex2Dlod(_WindMap, float4(windUV, 0, 0)).xy) * length(_WindVelocity);
				//Construct an axis from the wind sample to use for displacement
				float3 windAxis = normalize(float3(windSample.x, windSample.y, 0));
				//Wind displacement transformation matrix
				float3x3 windMatrix = angleAxis3x3(UNITY_PI * windSample, windAxis);

				//Random y-axis rotation
				float yAngle = rand(pos);
				yAngle = length(windSample) + (rand(pos) * 0.1);
				//Vertical rotation transformation matrix
				float3x3 randomRotationMatrix = angleAxis3x3(yAngle * UNITY_TWO_PI, float3(0, 0, 1.0f));

				//Random tilt rotation matrix
				float3x3 randomBendMatrix = angleAxis3x3(rand(pos.zzx) * _BendDelta * UNITY_PI * 0.5f, float3(-1.0f, 0.0f, 0.0f));

				//Object-based displacement
				float3 displacementDistance = distance(_DisplacementCenter, pos) * _DisplacementRadius;
				displacementDistance = saturate(1 - displacementDistance) * _DisplacementStrength;
				half3 comparison = mul(tangentToLocal, pos.xyz - _DisplacementCenter.xyz);
				displacementDistance = pow(clamp(displacementDistance, -1, 1), _DisplacementFalloff);
				//Object displacement transformation matrix
				float3x3 displacementTransformation = angleAxis3x3(displacementDistance, comparison);

				//Individual transform matrices for base and tip vertices (keeps base vertices anchored to ground plane
				float3x3 baseTransformation = mul(tangentToLocal, randomRotationMatrix);
				float3x3 tipTransformation = mul(mul(mul(mul(tangentToLocal, windMatrix), randomBendMatrix), randomRotationMatrix), displacementTransformation);

				//Calculate width, height, and bending per-blade
				float width = lerp(_BladeWidthMin, _BladeWidthMax, rand(pos.xxy) * falloff);
				float height = lerp(_BladeHeightMin, _BladeHeightMax, rand(pos.yyz) * falloff);
				float forward = rand(pos.zzx) * _BladeBendDistance;

				//Brightness Variation
				tint *= 1 - (rand(pos.xzy) * _LumaVariation) - 0.3f;
				//Hue Variation
				tint += float4(rand(pos.xyz) * _HueVariation, rand(pos.zyx) * _HueVariation, 0, 0);
				//Wind color variation
				tint += pow(length(windSample), 2) * 3;

				//Height color variation
				float fractionalHeight = ((height - _BladeHeightMin) / _BladeHeightMax);
				tint *= lerp(1, fractionalHeight, _HeightLuma);

				//Calculate atlas uv offsets
				_AtlasLength = floor(_AtlasLength);
				float uvScaling = 1 / _AtlasLength;
				//Calculate uv offset with general and height biases
				//General bias applies left-hand textures to more cards than right-hand textures
				//Height bias applies left-hand textures to shorter cards, right-hand textures to taller cards
				float uvOffset = round(pow(lerp(rand(pos.xyz), (fractionalHeight) - uvScaling, _AtlasHeightBias), _AtlasBias) * _AtlasLength) / _AtlasLength;

				//If blades are untapered, add one final segment to make up for missing last vertex
				float segmentCount = BLADE_SEGMENTS;
#ifndef TAPERED_BLADES
				segmentCount += 1;
#endif
				//Construct the vertices of the geometry
				for (int i = 0; i < segmentCount; ++i)
				{
					float t = i / (float)BLADE_SEGMENTS;
					float segmentWidth = width;

					//If blades are tapered, scale in each set of vertices to create a triangle shape
#ifdef TAPERED_BLADES
					segmentWidth = width * (1 - t);
#endif
					//Offsets for left and right side vertices
					float3 leftOffset = float3(segmentWidth, pow(t, _BladeBendCurve) * forward, height * t);
					float3 rightOffset = float3(-leftOffset.x, leftOffset.y, leftOffset.z);

					//If this is the first vertex pair, use the base transform
					float3x3 transformation = (i == 0) ? baseTransformation : tipTransformation;

					//Randomly flip uvs (by switching offsets on transforms
					if (rand(pos.xyz) > 0.5f)
					{
						triStream.Append(TransformGeometryToClip(pos, leftOffset, transformation, float2(0.0f + uvOffset, t), tint));
						triStream.Append(TransformGeometryToClip(pos, rightOffset, transformation, float2(uvScaling + uvOffset, t), tint));
					}
					else
					{
						triStream.Append(TransformGeometryToClip(pos, rightOffset, transformation, float2(0.0f + uvOffset, t), tint));
						triStream.Append(TransformGeometryToClip(pos, leftOffset, transformation, float2(uvScaling + uvOffset, t), tint));
					}

				}
				//If the blades are tapered, add in the final blade
#ifdef TAPERED_BLADES
				triStream.Append(TransformGeometryToClip(pos, float3(0.0f, forward, height), tipTransformation, float2((uvScaling * 0.5f) + uvOffset, 1.0f), tint));
#endif
				//Restart the strip for the next blade
				triStream.RestartStrip();
			}
		}
		ENDHLSL


		Pass {
			Name "Forward Shading"
			Tags { "LightMode" = "UniversalForward" }
			ZWrite On
			LOD 100
			Cull Off

			HLSLPROGRAM
	
			#pragma require geometry
			#pragma require tessellation tessHW

			//Since the main shader functions were declared in an HLSLINCLUDE, they can be easily referenced here
			#pragma vertex vert
			#pragma hull hull
			#pragma domain domain
			#pragma geometry geom
			#pragma fragment frag

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _SHADOWS_SOFT

			//Only the frag shader needs to be included, the rest are drawn from the HLSL include block above
			half4 frag(GeometryData i) : SV_Target 
			{
				//Color sampling and tint
				half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
				half4 color = baseMap * lerp(_BaseColor, _FadeColor, i.uv.y);
				color *= i.color;

				//Calculate shadow attenuation
#ifdef _MAIN_LIGHT_SHADOWS
				float4 ambient = float4(half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w), 1);

				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = i.worldPos;

				//Calculate shadow attenuation
				float4 shadowCoord = GetShadowCoord(vertexInput);
				half shadowAttenuation = saturate(MainLightRealtimeShadow(shadowCoord) + ambient);
				float4 shadowColor = lerp(0.0f, 1.0f, shadowAttenuation);

				//Multiply the adjusted shadow attenuation by the main light color for color and brightness
				Light mainLight = GetMainLight();
				shadowColor *= float4(mainLight.color, 1);

				color *= shadowColor;
#endif
				//Alpha clipping
				float alpha = baseMap.a;
				clip(alpha - _Cutoff);
				return half4(color.rgb, alpha);
			}
			ENDHLSL
		}


		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual
			//Set culling off to cast shadows from both sides of blades
			Cull Off

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x gles

			//We can reuse all but the frag function from the above include block
			#pragma target 4.6
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma hull hull
			#pragma domain domain
			#pragma multi_compile_shadowcaster

			#pragma shader_feature _ALPHATEST_ON
			#pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON

			//Because we've already used the vertex, tesselation, and geometry passes from above, the only unique pass is the frag which only needs to return a 0 (no transparency, pure geometry)
			float4 frag (GeometryData i) : SV_Target
			{
				float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).a;
				clip(alpha - _Cutoff);
				return alpha;
			}
			ENDHLSL
		}
	}
	CustomEditor "GeometryGrassGUI"
}