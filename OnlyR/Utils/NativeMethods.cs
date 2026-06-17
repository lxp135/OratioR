using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace OnlyR.Utils;

[ExcludeFromCodeCoverage]
internal static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetDiskFreeSpaceEx(
        [MarshalAs(UnmanagedType.LPWStr)] string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);
}