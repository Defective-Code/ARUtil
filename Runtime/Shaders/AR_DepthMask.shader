// Depth-only "occluder" shader for AR (URP).
// Writes depth but no colour, so the real camera image stays visible while any virtual content
// behind the mesh fails the depth test and is hidden.
//
// Drawn at queue Geometry-10 so it always lands in the depth buffer before your content.
// Content that uses a queue earlier than that (e.g. Background) will NOT be occluded.
//
// Built-in render pipeline version: delete the HLSL block and the "RenderPipeline" tag and use
//     Pass { ColorMask 0 ZWrite On }
Shader "AR/DepthMask"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DepthMask"
            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
