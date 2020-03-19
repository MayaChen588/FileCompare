using System;
using System.Collections.Generic;
using System.Text;

namespace FileCompare
{
    /// <summary>
    /// 檔案種類列舉
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// 未知
        /// </summary>
        None = 0,
        /// <summary>
        /// 二進位檔
        /// </summary>
        Binary,
        /// <summary>
        /// 文字檔
        /// </summary>
        Text,
    }
}
