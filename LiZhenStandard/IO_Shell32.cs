using LiZhenStandard.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace LiZhenStandard.IO
{
    public static class IO_Shell32
    {
        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SHFileOperation([In, Out] SHFILEOPSTRUCT str);
        private const int FO_MOVE = 0x0001;
        private const int FO_COPY = 0x0002;
        private const int FO_DELETE = 0x0003;
        private const int FO_RENAME = 0x0004;
        private const string SEPARATOR = "\0";
        private const ushort FOF_MULTIDESTFILES = 0x1;
        private const ushort FOF_NOCONFIRMATION = 0x10;
        private const ushort FOF_ALLOWUNDO = 0x40;

        /// <summary>
        /// 复制
        /// </summary>
        /// <param name="SourceFileName">文件源</param>
        /// <param name="DestFolderOrFileName">目标路径</param>
        /// <returns>是否成功复制</returns>
        public static bool FileOpration(string[] SourceFileNames, string DestFolderOrFileName, FileOpration opration)
        {
            SHFILEOPSTRUCT pm = new SHFILEOPSTRUCT();
            //pm.fFlags = FOF_ALLOWUNDO;//允许恢复 

            if (opration == IO.FileOpration.Copy)
            { pm.wFunc = FO_COPY; pm.lpszProgressTitle = "复制……"; }
            if (opration == IO.FileOpration.Move)
            { pm.wFunc = FO_MOVE; pm.lpszProgressTitle = "移动……"; }
            if (opration == IO.FileOpration.ReName)
            { pm.wFunc = FO_RENAME; pm.lpszProgressTitle = "重命名……"; }
            if (opration == IO.FileOpration.Delete)
            { pm.wFunc = FO_DELETE; pm.lpszProgressTitle = "删除……"; }

            pm.pFrom = SourceFileNames.AllToString(separator: SEPARATOR) + "\0\0";

            if (opration == IO.FileOpration.Copy || opration == IO.FileOpration.Move)
                if (!Directory.Exists(DestFolderOrFileName))
                    try
                    {
                        Directory.CreateDirectory(DestFolderOrFileName);
                    }
                    catch{ return false; }

            pm.pTo = DestFolderOrFileName + "\0\0";

            return !SHFileOperation(pm);
        }
    }

    public enum FileOpration { Copy,Move,ReName,Delete }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        /// <summary> 
        /// 设置操作方式，移动：FO_MOVE，复制：FO_COPY，删除：FO_DELETE 
        /// </summary> 
        public UInt32 wFunc;
        /// <summary> 
        /// 源文件路径 
        /// </summary> 
        public string pFrom;
        /// <summary> 
        /// 目标文件路径 
        /// </summary> 
        public string pTo;
        /// <summary> 
        /// 允许恢复 
        /// </summary> 
        public UInt16 fFlags;
        /// <summary> 
        /// 监测有无中止 
        /// </summary> 
        public Int32 fAnyOperationsAborted;
        public IntPtr hNameMappings;
        /// <summary> 
        /// 设置标题 
        /// </summary> 
        public string lpszProgressTitle;
    }

}
