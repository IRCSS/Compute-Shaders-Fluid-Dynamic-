#ifndef __FLUID_DYNAMIC_LIB__
#define __FLUID_DYNAMIC_LIB__

// -------------------------------------------------------------------------------------------------------------------------------------

// Globals that need to be defined before this include is added, these are in FluidDynamicCommonUniforms

// uint i_Resolution

//----------------------------------------------------------------------------
// Macro            : id2Dto1D
// Description      : This macro converts the 1D mapping of the structuredbuffer to the 2D grids of the vector field
//----------------------------------------------------------------------------
#define id2Dto1D(xy) (xy.x + (xy.y * i_Resolution.x)).x

//----------------------------------------------------------------------------
// Function         : Bilinear Structured Buffer Sampler
// Description      : Loads the 4 closest values on the grid centers around it and interpolates between them
//                    Since structured buffers are 1D, cache might not be as good as an actual Texture resource view sampler
//----------------------------------------------------------------------------
float4 StructuredBufferBilinearLoad(StructuredBuffer<float4> buffer, float2 coord) 
{
    float4 closest_grid_coords;

    closest_grid_coords.xy = floor(coord - 0.5) + 0.5;                  // Get the left and lower closest grid centers
    closest_grid_coords.zw = closest_grid_coords.xy + float2(1., 1.);   // Right, upper closest grid centers

    float2 lerp_factors    = coord - closest_grid_coords.xy;            // Get the fractional part of the actual sample position to the closest left-down sided grid center
    

    float4 left_down  = buffer[id2Dto1D(closest_grid_coords.xy)];
    float4 right_down = buffer[id2Dto1D(closest_grid_coords.zy)];
    float4 left_up    = buffer[id2Dto1D(closest_grid_coords.xw)];
    float4 right_up   = buffer[id2Dto1D(closest_grid_coords.zw)];


   return lerp(lerp(left_down, right_down, lerp_factors.x),             // Bilinear interpolation in x direction on the lower part
               lerp(left_up,   right_up,   lerp_factors.x),             // Bilinear interpolation in x direction on the upper part
               lerp_factors.y);                                         // Same but in y direction
}

// -------------------------------------------------------------------------------------------------------------------------------------

#endif
 