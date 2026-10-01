#ifndef SERUM_PLAYER_FOG_AREA_INCLUDED
#define SERUM_PLAYER_FOG_AREA_INCLUDED

// Global values supplied by the scene's PlayerFogArea component.
float4 _SerumFogAreaCenter;   // xyz: world center, w: enabled
float4 _SerumFogAreaExtents;  // xyz: half size, w: edge fade distance
float4 _SerumFogAreaSampling; // x: sample spacing, y: step cap, z: valid target

bool SerumFogClipAxis(float origin, float direction, float extent, inout float entry, inout float exitDistance)
{
    // Parallel rays must not divide by zero (including rays on a box face).
    if (abs(direction) < 1e-6)
        return abs(origin) <= extent;
    float a = (-extent - origin) / direction;
    float b = ( extent - origin) / direction;
    entry = max(entry, min(a, b));
    exitDistance = min(exitDistance, max(a, b));
    return exitDistance > entry;
}

bool SerumFogClipRay(float3 originWS, float3 directionWS, inout float entry, inout float exitDistance)
{
    if (_SerumFogAreaCenter.w < 0.5)
        return exitDistance > entry;
    if (_SerumFogAreaSampling.z < 0.5)
        return false;
    float3 origin = originWS - _SerumFogAreaCenter.xyz;
    if (!SerumFogClipAxis(origin.x, directionWS.x, _SerumFogAreaExtents.x, entry, exitDistance)) return false;
    if (!SerumFogClipAxis(origin.y, directionWS.y, _SerumFogAreaExtents.y, entry, exitDistance)) return false;
    return SerumFogClipAxis(origin.z, directionWS.z, _SerumFogAreaExtents.z, entry, exitDistance);
}

float SerumFogAreaMask(float3 positionWS)
{
    if (_SerumFogAreaCenter.w < 0.5)
        return 1.0;
    if (_SerumFogAreaSampling.z < 0.5)
        return 0.0;
    float3 edge = _SerumFogAreaExtents.xyz - abs(positionWS - _SerumFogAreaCenter.xyz);
    float nearestEdge = min(edge.x, min(edge.y, edge.z));
    if (nearestEdge <= 0.0)
        return 0.0;
    float fade = min(_SerumFogAreaExtents.w, min(_SerumFogAreaExtents.x, min(_SerumFogAreaExtents.y, _SerumFogAreaExtents.z)));
    return fade > 0.0 ? smoothstep(0.0, fade, nearestEdge) : 1.0;
}

int SerumFogStepCount(float distanceWS, int materialSteps)
{
    int count = max(1, materialSteps);
    if (_SerumFogAreaCenter.w >= 0.5)
    {
        int desired = (int)ceil(distanceWS / max(0.05, _SerumFogAreaSampling.x));
        count = max(1, min(count, min((int)_SerumFogAreaSampling.y, desired)));
    }
    return count;
}
#endif
