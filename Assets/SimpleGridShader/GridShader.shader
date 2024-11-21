Shader "PDT Shaders/TestGrid_URP"
{
    Properties
    {
        _LineColor ("Line Color", Color) = (1,1,1,1)
        _CellColor ("Cell Color", Color) = (0,0,0,0)
        _SelectedColor ("Selected Color", Color) = (1,0,0,1)
        [PerRendererData] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [Range(1,100)] _GridSize ("Grid Size", Float) = 10
        [Range(0,1)] _LineSize ("Line Size", Float) = 0.15
        [Range(0,1)] _SelectCell ("Select Cell Toggle", Float) = 0.0
        [Range(0,100)] _SelectedCellX ("Selected Cell X", Float) = 0.0
        [Range(0,100)] _SelectedCellY ("Selected Cell Y", Float) = 0.0
        _AlphaClip ("Alpha Clip Threshold", Range(0,1)) = 0.5
        _GridTiling ("Grid Tiling", Vector) = (1,1,0,0)
        _TextureTiling ("Texture Tiling", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend One Zero
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _LineColor;
            float4 _CellColor;
            float4 _SelectedColor;
            float _GridSize;
            float _LineSize;
            float _SelectCell;
            float _SelectedCellX;
            float _SelectedCellY;
            float _AlphaClip;

            float4 _GridTiling;
            float4 _TextureTiling;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv * _TextureTiling.xy + _TextureTiling.zw;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 tiledUV = input.uv * _GridTiling.xy + _GridTiling.zw;
                float2 id = floor(tiledUV * _GridSize);

                float4 color = _CellColor;
                float alpha = _CellColor.a;

                if (round(_SelectCell) == 1.0 && id.x == _SelectedCellX && id.y == _SelectedCellY)
                {
                    color = _SelectedColor;
                    alpha = _SelectedColor.a;
                }

                if (frac(tiledUV.x * _GridSize) < _LineSize || frac(tiledUV.y * _GridSize) < _LineSize)
                {
                    color = _LineColor;
                    alpha = _LineColor.a;
                }

                if (alpha < _AlphaClip)
                    discard;

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                color *= texColor;

                return half4(color.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
