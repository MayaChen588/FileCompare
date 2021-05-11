using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileCompare
{
    /// <summary>
    /// 比對檔案類別
    /// </summary>
    public class CompareFile
    {
        /// <summary>
        /// 檔案名稱
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 檔案所在基底路徑
        /// </summary>
        public string BasePath { get; set; }
        /// <summary>
        /// 檔案所在目錄路徑，不包含基底路徑
        /// </summary>
        public string PartPath { get; set; }
        /// <summary>
        /// 完整路徑
        /// </summary>
        public string FullPath
        { 
            get
            {
                return Path.Combine(BasePath, PartPath);
            }
        }
        /// <summary>
        /// 完整路徑檔名
        /// </summary>
        public string FilePath
        {
            get
            {
                return Path.Combine(BasePath, PartPath, FileName);
            }
        }
        /// <summary>
        /// 檔案種類
        /// </summary>
        public FileType FileType { get; set; }
        /// <summary>
        /// 比對差異類型
        /// </summary>
        public DiffKind DiffKind { get; set; }
        /// <summary>
        /// 檔案差異內容大小(byte)
        /// </summary>
        public long? FileDiffLength { get; set; }
    }
}
