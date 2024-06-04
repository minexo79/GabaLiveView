using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DaFenPlayer.Video.Utilities
{
    internal unsafe class UnsafeUtilities
    {
        public static string PtrToStringUTF8(byte* ptr)
        {
            return ptr != null ? Marshal.PtrToStringUTF8(new IntPtr(ptr)) : "";
        }
    }
}
