#ifndef __FLUID_DYNAMIC_LIB__
#define __FLUID_DYNAMIC_LIB__

// -------------------------------------------------------------------------------------------------------------------------------------

// Globals that need to be defined before this include is added, these are in FluidDynamicCommonUniforms

// uint i_Resolution

//----------------------------------------------------------------------------
// Macro            : id2Dto1D
// Description      : This macro converts the 1D mapping of the structuredbuffer to the 2D grids of the vector field
//----------------------------------------------------------------------------
//#define id2Dto1D(m_coord) (m_coord.x + (m_coord.y * (float)i_Resolution))        // I was getting to many compiler issues with a macro

int id2Dto1D(int2 m_coord) {
    return clamp(m_coord.x, 0, i_Resolution - 1 ) + clamp(m_coord.y, 0, i_Resolution - 1 ) * i_Resolution;
}


//----------------------------------------------------------------------------
// Function         : Bilinear Structured Buffer Sampler
// Description      : Loads the 4 closest values on the grid centers around it and interpolates between them
//                    Since structured buffers are 1D, cache might not be as good as an actual Texture resource view sampler
//----------------------------------------------------------------------------
float4 StructuredBufferBilinearLoad(StructuredBuffer<float4> buffer, float2 coord) 
{
    float4 closest_grid_coords;

    closest_grid_coords.xy = max(0.,round(coord - 0.5));                // Get the left and lower closest grid centers
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

//----------------------------------------------------------------------------
// Function         : gradient
// Description      : Calculates the gradient of a scalar field. The gradient is defined as
//                    the change of the vector field value in x and y direction. The scalar field
//                    here is passed as a float4 structured buffer, so that the same generic solver can be used
//                    for possion equation as well as the diffusion equation
//----------------------------------------------------------------------------
float4 gradient(StructuredBuffer<float4> scalar_field, float partial_xy, int2 coord) {
    
    float left     = scalar_field[id2Dto1D(coord - int2(1, 0))].x;
    float right    = scalar_field[id2Dto1D(coord + int2(1, 0))].x;
    float bottom   = scalar_field[id2Dto1D(coord - int2(0, 1))].x;
    float top      = scalar_field[id2Dto1D(coord + int2(0, 1))].x;

    return float4(right - left, top - bottom, 0.0, 0.0)  / partial_xy;

}

// -------------------------------------------------------------------------------------------------------------------------------------


#endif
 