using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static Lumina.Data.Files.TexFile;

namespace ZabCustomizer;

public unsafe partial class TextureCompressor : IDisposable
{
    private enum CompressionResult
    {
        Success = 0,
        InputError = 1,
        MipsError = 2,
        CompressionError = 3,
        SaveError = 4,
    }

#if DEBUG
    [LibraryImport("CompressionHelper\\x64_Debug\\ZabCompressionHelper.dll")]
#else
    [LibraryImport("CompressionHelper\\x64_Release\\ZabCompressionHelper.dll")]
#endif
    private static partial CompressionResult CompressTexture(char* sourceImageFilename, char* destinationTexFilename, ID3D11Device* d3d11Device);

    private readonly ID3D11Device* _device;
    private readonly SemaphoreSlim _gpuCompressionSemaphore = new(1);

    private TextureCompressor(ID3D11Device* device, string pluginDirectory)
    {
        _device = device;
    }

    public void CompressToTexFile(string inputFilename, string outputFilename)
    {
        _gpuCompressionSemaphore.Wait();
        try
        {
            fixed (char* inputFilenamePointer = inputFilename)
            fixed (char* outputFilenamePointer = outputFilename)
            {
                var result = CompressTexture(inputFilenamePointer, outputFilenamePointer, _device);
                if (result != CompressionResult.Success)
                {
                    throw new Exception("Failed to compress! Result: " + result.ToString());
                }
            }
        }
        finally
        {
            _gpuCompressionSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _device->Release();
        _gpuCompressionSemaphore.Dispose();
    }

    private static int Align(int value, int alignment)
    {
        return ((value + alignment - 1) / alignment) * alignment;
    }
    
    public static TextureCompressor Create(string pluginDirectory)
    {
        uint deviceFlags = 0;
#if DEBUG
        // Note that if you don't have the Graphics Tools feature installed in Windows, using this debug flag will cause device creation to fail.
        // If you are working on this code, you should grab that.
        deviceFlags |= (uint)D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_DEBUG;
#endif
        ID3D11Device* device = null;
        var result = DirectX.D3D11CreateDevice(pAdapter: null, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE, Software: HMODULE.NULL, deviceFlags, pFeatureLevels: null, FeatureLevels: 0, SDKVersion: D3D11.D3D11_SDK_VERSION, &device, pFeatureLevel: null, ppImmediateContext: null);
        if (result != S.S_OK)
        {
            throw new Exception("Failed to create D3D11 device.");
        }

        return new TextureCompressor(device, pluginDirectory);
    }
}
