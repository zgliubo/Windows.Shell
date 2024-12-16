using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Shell
{
    internal static class OsVersion
    {
        /// <summary>
        ///  Version info structure for <see cref="RtlGetVersion(out RTL_OSVERSIONINFOEX)" />
        /// </summary>
        /// <remarks>
        ///  Note that this structure is the exact same defintion as OSVERSIONINFOEX.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            internal fixed char szCSDVersion[128];
            internal ushort wServicePackMajor;
            internal ushort wServicePackMinor;
            internal ushort wSuiteMask;
            internal byte wProductType;
            internal byte wReserved;
        }

        [DllImport("ntdll.dll", EntryPoint = "RtlGetVersion", ExactSpelling = true)]
        private static extern int RtlGetVersionInternal(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        internal static unsafe int RtlGetVersion(out RTL_OSVERSIONINFOEX versionInfo)
        {
            versionInfo = new RTL_OSVERSIONINFOEX
            {
                dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOEX)
            };
            return RtlGetVersionInternal(ref versionInfo);
        }

        private static RTL_OSVERSIONINFOEX s_versionInfo = InitVersion();

        private static RTL_OSVERSIONINFOEX InitVersion()
        {
            // We use RtlGetVersion as it isn't subject to version lie. GetVersion
            // won't tell you the real version unless the launching exe is manifested
            // with the latest OS version.

            RtlGetVersion(out RTL_OSVERSIONINFOEX info);
            return info;
        }

        /// <summary>
        ///  Is Windows 10 first release or later. (Threshold 1, build 10240, version 1507)
        /// </summary>
        public static bool IsWindows10_1507OrGreater
            => s_versionInfo.dwMajorVersion >= 10 && s_versionInfo.dwBuildNumber >= 10240;

        /// <summary>
        ///  Is Windows 10 Anniversary Update or later. (Redstone 1, build 14393, version 1607)
        /// </summary>
        public static bool IsWindows10_1607OrGreater
            => s_versionInfo.dwMajorVersion >= 10 && s_versionInfo.dwBuildNumber >= 14393;

        /// <summary>
        ///  Is Windows 10 Creators Update or later. (Redstone 2, build 15063, version 1703)
        /// </summary>
        public static bool IsWindows10_1703OrGreater
            => s_versionInfo.dwMajorVersion >= 10 && s_versionInfo.dwBuildNumber >= 15063;

        /// <summary>
        ///  Is this Windows 11 public preview or later?
        ///  The underlying API does not read supportedOs from the manifest, it returns the actual version.
        /// </summary>
        public static bool IsWindows11_OrGreater
            => s_versionInfo.dwMajorVersion >= 10 && s_versionInfo.dwBuildNumber >= 22000;

        /// <summary>
        ///  Is Windows 8.1 or later.
        /// </summary>
        public static bool IsWindows8_1OrGreater
            => s_versionInfo.dwMajorVersion >= 10
                || (s_versionInfo.dwMajorVersion == 6 && s_versionInfo.dwMinorVersion == 3);

        /// <summary>
        ///  Is Windows 8 or later.
        /// </summary>
        public static bool IsWindows8OrGreater
            => s_versionInfo.dwMajorVersion >= 8;
    }
}
