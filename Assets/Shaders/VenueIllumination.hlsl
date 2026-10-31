#ifndef BIATHLON_VENUE_ILLUMINATION
#define BIATHLON_VENUE_ILLUMINATION
// Stationary lamps are integrated once at load into a world-space diffuse field.
// Daytime bypasses the texture read. Geometry, textures, normals and shadows stay intact.
TEXTURE2D(_VenueLightMap); SAMPLER(sampler_VenueLightMap);
float4 _VenueLightBounds;
float _VenueLightGain, _VenueLightHeight;
half3 VenueIllumination(float3 positionWS, half3 normalWS)
{
    half3 result=half3(0,0,0);
    [branch] if (_VenueLightGain > 0)
    {
        float2 uv=(positionWS.xz-_VenueLightBounds.xy)*_VenueLightBounds.zw;
        half4 field=SAMPLE_TEXTURE2D(_VenueLightMap,sampler_VenueLightMap,uv);
        half inside=step(0,uv.x)*step(0,uv.y)*step(uv.x,1)*step(uv.y,1);
        half height=saturate(1-abs(positionWS.y-field.a)/max(1,_VenueLightHeight));
        result=field.rgb * (_VenueLightGain*inside*height) * (.45h+.55h*saturate(normalWS.y));
    }
    return result;
}
#endif
