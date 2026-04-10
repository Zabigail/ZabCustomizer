#pragma once

#include <cstdint>

//
// Quickly ported from Lumina
//

enum class TexAttribute : uint32_t
{
    DiscardPerFrame = 1u,
    DiscardPerMap = 2u,
    Managed = 4u,
    UserManaged = 8u,
    CpuRead = 0x10u,
    LocationMain = 0x20u,
    NoGpuRead = 0x40u,
    AlignedSize = 0x80u,
    EdgeCulling = 0x100u,
    LocationOnion = 0x200u,
    ReadWrite = 0x400u,
    Immutable = 0x800u,
    TextureRenderTarget = 0x100000u,
    TextureDepthStencil = 0x200000u,
    TextureType1D = 0x400000u,
    TextureType2D = 0x800000u,
    TextureType3D = 0x1000000u,
    TextureType2DArray = 0x10000000u,
    TextureTypeCube = 0x2000000u,
    TextureTypeMask = 0x13C00000u,
    TextureSwizzle = 0x4000000u,
    TextureNoTiled = 0x8000000u,
    TextureNoSwizzle = 0x80000000u
};

enum class TexFormat : uint32_t
{
    TypeShift = 0xC,
    TypeMask = 0xF000,
    ComponentShift = 8,
    ComponentMask = 0xF00,
    BppShift = 4,
    BppMask = 0xF0,
    EnumShift = 0,
    EnumMask = 0xF,
    TypeInteger = 1,
    TypeFloat = 2,
    TypeDxt = 3,
    TypeBc123 = 3,
    TypeDepthStencil = 4,
    TypeSpecial = 5,
    TypeBc57 = 6,
    Unknown = 0,
    L8 = 0x1130,
    A8 = 0x1131,
    B4G4R4A4 = 0x1440,
    B5G5R5A1 = 0x1441,
    B8G8R8A8 = 0x1450,
    B8G8R8X8 = 0x1451,
    //[Obsolete("Use B4G4R4A4 instead.")]
    //R4G4B4A4 = 0x1440,
    //[Obsolete("Use B5G5R5A1 instead.")]
    //R5G5B5A1 = 0x1441,
    //[Obsolete("Use B8G8R8A8 instead.")]
    //A8R8G8B8 = 0x1450,
    //[Obsolete("Use B8G8R8X8 instead.")]
    //R8G8B8X8 = 0x1451,
    //[Obsolete("Not supported by Windows DirectX 11 version of the game, nor have any mention of the value, as of 6.15.")]
    //A8R8G8B82 = 0x1452,
    R32F = 0x2150,
    R16G16F = 0x2250,
    R32G32F = 0x2260,
    R16G16B16A16F = 0x2460,
    R32G32B32A32F = 0x2470,
    DXT1 = 0x3420,
    DXT3 = 0x3430,
    DXT5 = 0x3431,
    ATI2 = 0x6230,
    BC1 = 0x3420,
    BC2 = 0x3430,
    BC3 = 0x3431,
    BC4 = 0x6120,
    BC5 = 0x6230,
    BC6H = 0x6330,
    BC7 = 0x6432,
    D16 = 0x4140,
    D24S8 = 0x4250,
    Null = 0x5100,
    Shadow16 = 0x5140,
    Shadow24 = 0x5150
};

struct TexHeader
{
public:
    TexAttribute Type;
    TexFormat Format;
    uint16_t Width;
    uint16_t Height;
    uint16_t Depth;
    uint8_t MipCount : 7;
    uint8_t MipUnknownFlag : 1;
    uint8_t ArraySize;
    uint32_t LodOffset[3];
    uint32_t OffsetToSurface[13];
};
