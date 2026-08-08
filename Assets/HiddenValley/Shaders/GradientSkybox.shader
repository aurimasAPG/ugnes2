// Three-stop gradient skybox. The horizon color is set equal to the fog color by
// AtmosphereRig, which is the single trick that makes a primitive world read as a
// place: geometry fades into exactly the sky it stands under.
Shader "HiddenValley/GradientSkybox"
{
    Properties
    {
        _Top ("Top", Color) = (0.35, 0.45, 0.58, 1)
        _Horizon ("Horizon", Color) = (0.70, 0.74, 0.78, 1)
        _Bottom ("Bottom", Color) = (0.42, 0.44, 0.46, 1)
        _HorizonSharpness ("Horizon Sharpness", Range(0.5, 8)) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Top;
            fixed4 _Horizon;
            fixed4 _Bottom;
            float _HorizonSharpness;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;
                float up = saturate(y);
                float down = saturate(-y);
                fixed4 sky = lerp(_Horizon, _Top, pow(up, 1.0 / _HorizonSharpness));
                fixed4 ground = lerp(_Horizon, _Bottom, pow(down, 0.7));
                return y >= 0 ? sky : ground;
            }
            ENDCG
        }
    }
}
