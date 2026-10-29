// Redraws a candidate surface with everything outside the Paint Volume clipped away, so the user
// can see exactly which geometry will receive points.
//
// Unlit, with no LightMode tag, and only ever drawn through Graphics.DrawMeshNow + SetPass from
// duringSceneGui. That is immediate mode and sits outside the render pipeline's own loop, which is
// why it works under built-in, URP and HDRP alike - the same reason the ghost preview does.
Shader "Cromware/PatternPainter/VolumeClipHighlight"
{
    Properties
    {
        _Color ("Color", Color) = (0,0.78,0.99,0.35)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Offset -1, -1   // win the depth fight with the surface being overlaid
        Cull Back
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float4x4 _VolumeWorldToLocal;
            float3 _VolumeHalfSize;
            float _VolumeShape;   // 0 = box, 1 = cylinder

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 local = mul(_VolumeWorldToLocal, float4(i.worldPos, 1.0)).xyz;

                bool insideBox = all(abs(local) <= _VolumeHalfSize);
                bool insideCylinder = abs(local.y) <= _VolumeHalfSize.y &&
                    dot(local.xz, local.xz) <= _VolumeHalfSize.x * _VolumeHalfSize.x;

                bool inside = _VolumeShape < 0.5 ? insideBox : insideCylinder;
                clip(inside ? 1 : -1);

                return _Color;
            }
            ENDCG
        }
    }
}
