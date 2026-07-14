using Godot;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CaptureTheSpire.CaptureTheSpireCode;

internal static partial class WindowsClipboard
{
    private const uint CfDib = 8;
    private const uint GmemMoveable = 0x0002;
    private const uint BiRgb = 0;
    private const int BitmapInfoHeaderSize = 40;

    internal static bool TryCopy(Image image, out string? error)
    {
        error = null;

        if (!OperatingSystem.IsWindows())
        {
            error = "Image clipboard copying is currently supported only on Windows.";
            return false;
        }

        try
        {
            Copy(image);

            if (!DisplayServer.ClipboardHasImage())
            {
                error = "Windows accepted the clipboard data, but Godot could not detect an image afterward.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            return false;
        }
    }

    private static void Copy(Image image)
    {
        var rgbaImage = image.Duplicate() as Image ?? throw new InvalidOperationException("Could not duplicate the captured image.");

        rgbaImage.Convert(Image.Format.Rgba8);

        var width = rgbaImage.GetWidth();
        var height = rgbaImage.GetHeight();
        var rgba = rgbaImage.GetData();
        var dib = CreateDeviceIndependentBitmap(rgba, width, height);

        var windowHandle = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);

        if (windowHandle == 0)
            throw new InvalidOperationException("Could not get the native game window handle.");

        if (!TryOpenClipboard(windowHandle))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not open the Windows clipboard.");

        nint memoryHandle = 0;

        try
        {
            if (!EmptyClipboard())
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not clear the Windows clipboard.");

            memoryHandle = GlobalAlloc(GmemMoveable, (nuint)dib.Length);

            if (memoryHandle == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not allocate clipboard memory.");

            var memoryPointer = GlobalLock(memoryHandle);

            if (memoryPointer == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not lock clipboard memory.");

            try
            {
                Marshal.Copy(dib, 0, memoryPointer, dib.Length);
            }
            finally
            {
                GlobalUnlock(memoryHandle);
            }

            var clipboardHandle = SetClipboardData(CfDib, memoryHandle);

            if (clipboardHandle == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not place the image on the clipboard.");

            memoryHandle = 0;
        }
        finally
        {
            CloseClipboard();

            if (memoryHandle != 0)
                GlobalFree(memoryHandle);
        }
    }

    private static byte[] CreateDeviceIndependentBitmap(byte[] rgba, int width, int height)
    {
        var rowSize = width * 4;
        var pixelDataSize = rowSize * height;
        var dib = new byte[BitmapInfoHeaderSize + pixelDataSize];

        WriteInt32(dib, 0, BitmapInfoHeaderSize);
        WriteInt32(dib, 4, width);
        WriteInt32(dib, 8, height);
        WriteUInt16(dib, 12, 1);
        WriteUInt16(dib, 14, 32);
        WriteUInt32(dib, 16, BiRgb);
        WriteUInt32(dib, 20, (uint)pixelDataSize);

        for (var sourceY = 0; sourceY < height; sourceY++)
        {
            var destinationY = height - 1 - sourceY;

            for (var x = 0; x < width; x++)
            {
                var sourceOffset = (sourceY * width + x) * 4;
                var destinationOffset = BitmapInfoHeaderSize + (destinationY * width + x) * 4;

                dib[destinationOffset] = rgba[sourceOffset + 2];
                dib[destinationOffset + 1] = rgba[sourceOffset + 1];
                dib[destinationOffset + 2] = rgba[sourceOffset];
                dib[destinationOffset + 3] = 0;
            }
        }

        return dib;
    }

    private static bool TryOpenClipboard(nint windowHandle)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (OpenClipboard(windowHandle))
                return true;

            Thread.Sleep(25);
        }

        return false;
    }

    private static void WriteUInt16(byte[] destination, int offset, ushort value)
    {
        BitConverter.TryWriteBytes(destination.AsSpan(offset, sizeof(ushort)), value);
    }

    private static void WriteInt32(byte[] destination, int offset, int value)
    {
        BitConverter.TryWriteBytes(destination.AsSpan(offset, sizeof(int)), value);
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        BitConverter.TryWriteBytes(destination.AsSpan(offset, sizeof(uint)), value);
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint windowHandle);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint SetClipboardData(uint format, nint memoryHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalAlloc(uint flags, nuint bytes);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalLock(nint memoryHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint memoryHandle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GlobalFree(nint memoryHandle);
}