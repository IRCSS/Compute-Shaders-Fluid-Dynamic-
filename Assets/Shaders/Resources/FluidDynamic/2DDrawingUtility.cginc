#ifndef __2D_DRAWING_UTILITY__
#define __2D_DRAWING_UTILITY__

// This function is used to a line between two points. The points are defined through point A and Point A + direction* length
float DrawHalfVectorWithLength(float2 origin, float2 line_direction, float len, float2 uv, float size, float smoothness){
    
           uv  -= origin;
    float  v2   = dot(line_direction, line_direction);
    float  vUv  = dot(line_direction, uv);
    float2 p    = line_direction * vUv/v2;
    float  d    = distance(p, uv);
    float  m    = 1. - step(0.,vUv/v2);
           m   += smoothstep(len, len + smoothness/2., vUv/v2);
    return 1. - clamp(smoothstep(size, size + smoothness, d) + m, 0. ,1.);
}

#endif