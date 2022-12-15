

float4 getVertexInClipSpace(float3 pos) {
    #ifdef USING_URP
        return mul (UNITY_MATRIX_VP, mul (UNITY_MATRIX_M, float4 (pos,1.0)));
    #else
        return UnityObjectToClipPos(pos);
    #endif
}
