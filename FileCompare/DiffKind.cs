using System;
using System.Collections.Generic;
using System.Text;

namespace FileCompare
{
    /// <summary>
    /// 差異類型列舉
    /// </summary>
    public enum DiffKind
    {
        /// <summary>
        /// 未知
        /// </summary>
        None = 0,
        /// <summary>
        /// 新增
        /// </summary>
        New,
        /// <summary>
        /// 變更
        /// </summary>
        Changed,
        /// <summary>
        /// 沒有變更
        /// </summary>
        Unchanged,
        /// <summary>
        /// 刪除
        /// </summary>
        Deleted,
    }
}
