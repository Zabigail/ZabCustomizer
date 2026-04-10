// dllmain.cpp : Defines the entry point for the DLL application.
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <dxgi.h>

#include "DirectXTex.h"

#include "TexFile.h"

/**
 * @brief The various possible results of compression.
 */
enum class CompressionResult
{
    /**
     * @brief Indicates that the input image was successfully compressed and saved to the destination path.
     */
    Success = 0,

    /**
     * @brief Indicates that there was a failure attempting to load the input image file.
     */
    InputError = 1,

    /**
     * @brief Indicates that there was a failure attempting to generate a full chain of mipmaps for the texture.
     */
    MipsError = 2,

    /**
     * @brief Indicates that there was a failure attempting compress the pixels into the BC7 format with the GPU.
     */
    CompressionError = 3,

    /**
     * @brief Indicates that there was a failure attempting to save the generated .tex file to the given destination path.
     */
    SaveError = 4,
};

/**
 * @brief Compresses an image file at the given path into a compressed FFXIV .tex file.
 * 
 * The source image is compressed into a BC7 texture using the GPU.
 * 
 * @param sourceImageFilename The full path to the source image file to compress. Valid file types are any WIC-supported file (e.g. PNG).
 * @param destinationTexFilename The full path to save the resulting .tex file at.
 * @param d3d11Device The Direct3D11 device to use when compressing.
 * 
 * @return A result code indicating success or failure.
 */
extern "C" __declspec(dllexport) CompressionResult CompressTexture(const wchar_t* sourceImageFilename, const wchar_t* destinationTexFilename, ID3D11Device* d3d11Device)
{
    // Load the source image
    DirectX::TexMetadata metadata = { };
    DirectX::ScratchImage inputImage = { };
    HRESULT hr = DirectX::LoadFromWICFile(sourceImageFilename, DirectX::WIC_FLAGS_NONE, &metadata, inputImage);
    if (FAILED(hr))
    {
        return CompressionResult::InputError;
    }

    // Generate a full mip chain
    DirectX::ScratchImage inputImageWithMips = { };
    hr = DirectX::GenerateMipMaps(*inputImage.GetImage(0, 0, 0), DirectX::TEX_FILTER_FLAGS::TEX_FILTER_DEFAULT, 0, inputImageWithMips);
    if (FAILED(hr))
    {
        return CompressionResult::MipsError;
    }

    // Compress to BC7
    DirectX::ScratchImage compressedImageWithMips = { };
    hr = DirectX::Compress((ID3D11Device*)d3d11Device, inputImageWithMips.GetImages(), inputImageWithMips.GetImageCount(), inputImageWithMips.GetMetadata(), DXGI_FORMAT::DXGI_FORMAT_BC7_UNORM, DirectX::TEX_COMPRESS_FLAGS::TEX_COMPRESS_DEFAULT, 0.5f, compressedImageWithMips);
    if (FAILED(hr))
    {
        return CompressionResult::CompressionError;
    }

    // Initialize the output .tex header
    TexHeader newHeader =
    {
        TexAttribute::TextureType2D,
        TexFormat::BC7,
        metadata.width,
        metadata.height,
        1, // Depth
        metadata.mipLevels,
        0, // MipUnknownFlag
        0, // ArraySize
    };

    newHeader.LodOffset[0] = 0;
    newHeader.LodOffset[1] = 1;
    newHeader.LodOffset[2] = 2;

    // Compute the offsets of each of the mips, and thus the size of the whole file
    uint32_t mipOffset = sizeof(TexHeader);
    for (int mipIndex = 0; mipIndex < newHeader.MipCount; mipIndex++)
    {
        newHeader.OffsetToSurface[mipIndex] = mipOffset;

        for (int z = 0; z < newHeader.Depth; z++)
        {
            mipOffset += compressedImageWithMips.GetImage(mipIndex, 0, z)->slicePitch;
        }
    }

    // Zero out the remaining mip offset slots
    for (int mipIndex = newHeader.MipCount; mipIndex < 13; mipIndex++)
    {
        newHeader.OffsetToSurface[mipIndex] = 0;
    }

    // Generate the tex bytes
    char* outputTexBytes = new char[mipOffset];
    *(TexHeader*)outputTexBytes = newHeader;
    for (int mipIndex = 0; mipIndex < newHeader.MipCount; mipIndex++)
    {
        for (int z = 0; z < newHeader.Depth; z++)
        {
            const DirectX::Image* mipImage = compressedImageWithMips.GetImage(mipIndex, 0, z);
            memcpy(outputTexBytes + newHeader.OffsetToSurface[mipIndex] + z * mipImage->slicePitch, mipImage->pixels, mipImage->slicePitch);
        }
    }

    // Write the tex to disk
    CompressionResult result = CompressionResult::Success;
    HANDLE outFileHandle = CreateFileW(destinationTexFilename, GENERIC_WRITE, 0, nullptr, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (outFileHandle != INVALID_HANDLE_VALUE)
    {
        DWORD bytesWritten = 0;
        result = WriteFile(outFileHandle, outputTexBytes, mipOffset, &bytesWritten, nullptr) ? CompressionResult::Success : CompressionResult::SaveError;

        CloseHandle(outFileHandle);
    }
    else
    {
        result = CompressionResult::SaveError;
    }

    delete[] outputTexBytes;
    return result;
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

