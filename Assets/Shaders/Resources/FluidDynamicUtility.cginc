#ifndef __FLUID_DYNAMIC_LIB__
#define __FLUID_DYNAMIC_LIB__

// -------------------------------------------------------------------------------------------------------------------------------------

// Globals that need to be defined before this include is added, these are in FluidDynamicCommonUniforms

// uint i_Resolution

// Convert the x,y texture like coordinate to the one dimensional array which is the structured buffer
#define id2Dto1D(xy) (xy.x + (xy.y * i_Resolution.x)).x


// -------------------------------------------------------------------------------------------------------------------------------------

#endif
 