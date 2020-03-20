using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileCompare
{
    /// <summary>
    /// 檔案差異比對主程式類別
    /// 提供比對二個指定的目錄下檔案，並產出差異清單及檔案差異報表
    /// Created by maya.chen@tpinformation.com.tw
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // SourcePath: 比對來源路徑
            // TargetPath: 比對標的路徑
            // ResultPath: 比對結果輸出目錄
            // GenDiffReport: 是否產出檔案差異報表

            if (!CheckArgument(args))
            {
                Environment.Exit(-1);
                return;
            }

            SortedList<string, CompareFile> filelist = new SortedList<string, CompareFile>();
            string sourcePath = null;
            string targetPath = null;
            string resultPath = null;
            bool outputDiffFile = false;

            try
            {
                if (!Directory.Exists(Path.GetFullPath(args[2]).TrimEnd(Path.DirectorySeparatorChar)))
                {
                    Directory.CreateDirectory(Path.GetFullPath(args[2]).TrimEnd(Path.DirectorySeparatorChar));
                }
            }
            catch (Exception ex)
            {
                OutputMessage($"Create result folder fail.", true);
                OutputMessage(ex.Message, true);
                Environment.Exit(-1);
                return;
            }

            try
            {
                sourcePath = Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar);
                targetPath = Path.GetFullPath(args[1]).TrimEnd(Path.DirectorySeparatorChar);
                resultPath = Path.GetFullPath(args[2]).TrimEnd(Path.DirectorySeparatorChar);
                outputDiffFile = Convert.ToBoolean(args[3]);

                OutputMessage("Collect file list, please waiting...", true);

                FillFileList(filelist, sourcePath, sourcePath);
                FillFileList(filelist, targetPath, targetPath);
            }
            catch (Exception ex)
            {
                OutputMessage($"Fill file list fail.", true);
                OutputMessage(ex.Message, true);
                Environment.Exit(-1);
                return;
            }


            try
            {
                OutputMessage("\r\n", true);
                OutputMessage("Compare file...", true);
                Compare(filelist, sourcePath, targetPath, resultPath, outputDiffFile);

                OutputMessage("\r\n", true);
                OutputMessage("Generate CompareList.csv ...", true);
                GenCompareList(filelist, resultPath);
                OutputMessage("  => OK", true);

                OutputMessage("\r\n", true);
                OutputCompareSummary(filelist);
            }
            catch (Exception ex)
            {
                OutputMessage(ex.Message, true);
                Environment.Exit(-1);
                return;
            }

            Environment.Exit(0);
        }


        /// <summary>
        /// 依指定目錄路徑生成檔案清單，若目錄下檔案在清單已存在則忽略
        /// </summary>
        /// <param name="list">檔案清單</param>
        /// <param name="basePath">來源基底目錄路徑</param>
        /// <param name="path">來源目錄路徑</param>
        private static void FillFileList(SortedList<string, CompareFile> list, string basePath, string path)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (!Directory.Exists(basePath))
            {
                throw new InvalidOperationException($"[{basePath}] path not exists");
            }

            if (!Directory.Exists(path))
            {
                throw new InvalidOperationException($"[{path}] path not exists");
            }


            DirectoryInfo currentDir = new DirectoryInfo(path);

            foreach (FileInfo file in currentDir.GetFiles())
            {
                string filePartPathName = file.FullName.Replace(basePath, "", StringComparison.OrdinalIgnoreCase).TrimStart(Path.DirectorySeparatorChar);

                if (!list.ContainsKey(filePartPathName))
                {
                    //OutputMessage($"Add file list: {String.Format("{0:D5}", list.Count + 1)} {file.FullName} ...", true);

                    list.Add(filePartPathName,
                        new CompareFile()
                        {
                            FileName = file.Name,
                            BasePath = basePath,
                            PartPath = file.DirectoryName.Replace(basePath, "", StringComparison.OrdinalIgnoreCase)
                                            .TrimStart(Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar),
                            FileType = GetFileType(file.FullName),
                        });
                }
            }


            foreach (DirectoryInfo subdir in currentDir.GetDirectories())
            {
                FillFileList(list, basePath, subdir.FullName);
            }
        }


        /// <summary>
        /// 取得檔案種類
        /// </summary>
        /// <param name="filePath">檔案路徑及檔名</param>
        /// <returns>檔案種類</returns>
        private static FileType GetFileType(string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                cdet.Feed(fs);
                cdet.DataEnd();

                if (!String.IsNullOrEmpty(cdet.Charset))
                {
                    return FileType.Text;
                }
                else
                {
                    return FileType.Binary;
                }
            }
        }


        /// <summary>
        /// 依檔案清單比對二個指定目錄路徑下檔案
        /// </summary>
        /// <param name="list">比對檔案清單</param>
        /// <param name="sourcePath">來源路徑</param>
        /// <param name="targetPath">對象路徑</param>
        /// <param name="resultPath">比對結果輸出路徑</param>
        /// <param name="outputDiffFile">是否產出差異檔案</param>
        private static void Compare(SortedList<string, CompareFile> list, string sourcePath, string targetPath, string resultPath, bool outputDiffFile)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            if (!Directory.Exists(sourcePath))
            {
                throw new InvalidOperationException($"[{sourcePath}] path not exists");
            }

            if (!Directory.Exists(targetPath))
            {
                throw new InvalidOperationException($"[{sourcePath}] path not exists");
            }


            if (!Directory.Exists(resultPath))
            {
                Directory.CreateDirectory(resultPath);
            }


            int count = 0;
            string sourceFilePath = null;
            string targetFilePath = null;
            string diffFilePath = null;


            foreach (var item in list)
            {
                count++;
                OutputMessage($"{String.Format("{0:D6}", count)} {item.Key}", true);

                sourceFilePath = Path.Combine(sourcePath, item.Value.PartPath, item.Value.FileName);
                targetFilePath = Path.Combine(targetPath, item.Value.PartPath, item.Value.FileName);

                if (File.Exists(sourceFilePath) &&
                    File.Exists(targetFilePath))
                {
                    if (GetCheckSum(sourceFilePath).Equals(GetCheckSum(targetFilePath)))
                    {
                        item.Value.DiffKind = DiffKind.Unchanged;
                    }
                    else
                    {
                        item.Value.DiffKind = DiffKind.Changed;
                    }
                }
                else if (File.Exists(sourceFilePath))
                {
                    item.Value.DiffKind = DiffKind.Deleted;
                }
                else if (File.Exists(targetFilePath))
                {
                    item.Value.DiffKind = DiffKind.New;
                }


                if (outputDiffFile &&
                    item.Value.DiffKind == DiffKind.Changed &&
                    item.Value.FileType == FileType.Text)
                {
                    diffFilePath = Path.Combine(resultPath, "DiffFile", item.Value.PartPath, item.Value.FileName);
                    CompareDiff(sourceFilePath, targetFilePath, diffFilePath);
                }
            }
        }


        /// <summary>
        /// 取得檔案驗證檢查碼
        /// </summary>
        /// <param name="filePath">檔案路徑檔名</param>
        /// <returns>檔案驗證檢查碼</returns>
        private static string GetCheckSum(string filePath)
        {
            byte[] checksum = null;

            using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                checksum = md5.ComputeHash(file);
            }

            return BitConverter.ToString(checksum).Replace("-", "");
        }


        /// <summary>
        /// 比對檔案產生差異檔案
        /// </summary>
        /// <param name="sourceFile">來源檔案路徑檔名</param>
        /// <param name="targetFile">對象檔案路徑檔名</param>
        /// <param name="diffFile">差異檔案路徑檔名</param>
        private static void CompareDiff(string sourceFile, string targetFile, string diffFile)
        {
            // 使用DiffUtils工具diff.exe，程式放置在程式目錄下diff目錄
            string workdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diff");

            if (!File.Exists(Path.Combine(workdir, "diff.exe")))
            {
                throw new InvalidOperationException($"{Path.Combine(workdir, "diff.exe")} not exists!");
            }


            if (!Directory.Exists(Path.GetDirectoryName(diffFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(diffFile));
            }

            using (Process p = new Process())
            {
                //ProcessStartInfo processInfo = new ProcessStartInfo();
                //processInfo.FileName = Path.Combine(workdir, "diff.exe");
                //processInfo.Arguments = $"-U 0 {sourceFile} {targetFile}";
                //processInfo.WorkingDirectory = "";
                //processInfo.CreateNoWindow = true;
                //processInfo.UseShellExecute = false;
                //processInfo.RedirectStandardOutput = true;
                //processInfo.RedirectStandardError = true;
                //processInfo.StandardOutputEncoding = Encoding.UTF8;
                //processInfo.StandardErrorEncoding = Encoding.UTF8;

                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = "cmd.exe";
                processInfo.WorkingDirectory = "";
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardInput = true;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;

                p.StartInfo = processInfo;
                p.Start();
                p.StandardInput.WriteLine($"{Path.Combine(workdir, "diff.exe")} -U 0 {sourceFile} {targetFile} > {diffFile}");

                //string outputmsg = p.StandardOutput.ReadToEnd();
                //string errmsg = p.StandardError.ReadToEnd();

                p.StandardInput.WriteLine("exit");
                p.WaitForExit(300 * 1000);

                p.Close();
            }
        }


        /// <summary>
        /// 產生檔案比對結果清單
        /// </summary>
        /// <param name="list">比對檔案清單</param>
        /// <param name="resultPath">比對結果輸出路徑</param>
        private static void GenCompareList(SortedList<string, CompareFile> list, string resultPath)
        {
            string resultFile = Path.Combine(resultPath, "CompareList.csv");

            using (StreamWriter file = new StreamWriter(resultFile, false, Encoding.UTF8))
            {
                string diffKind = String.Empty;
                string fileType = String.Empty;

                file.WriteLine($"異動種類,程式種類,檔案路徑,檔案名稱,檔案種類");

                foreach (var item in list)
                {
                    if (item.Value.DiffKind == DiffKind.New)
                    {
                        diffKind = "ADD";
                    }
                    else if (item.Value.DiffKind == DiffKind.Changed)
                    {
                        diffKind = "MOD";
                    }
                    else if (item.Value.DiffKind == DiffKind.Deleted)
                    {
                        diffKind = "DEL";
                    }
                    else
                    {
                        diffKind = "";
                    }

                    if (item.Value.FileType == FileType.Binary)
                    {
                        fileType = "Binary";
                    }
                    else if (item.Value.FileType == FileType.Text)
                    {
                        fileType = "Text";
                    }
                    else
                    {
                        fileType = "";
                    }


                    file.WriteLine($"{diffKind},{"SRC"},{item.Value.PartPath},{item.Value.FileName},{fileType}");
                }

                file.Close();
            }
        }


        /// <summary>
        /// 輸出比對結果
        /// </summary>
        /// <param name="list">比對檔案清單</param>
        private static void OutputCompareSummary(SortedList<string, CompareFile> list)
        {
            int totalFileCnt = 0;
            int equalFileCnt = 0;
            int diffFileCnt = 0;
            int newFileCnt = 0;
            int deleteFileCnt = 0;
            int unknowFileCnt = 0;

            foreach (var item in list)
            {
                totalFileCnt++;

                if (item.Value.DiffKind == DiffKind.Changed)
                {
                    diffFileCnt++;
                }
                else if (item.Value.DiffKind == DiffKind.New)
                {
                    newFileCnt++;
                }
                else if (item.Value.DiffKind == DiffKind.Deleted)
                {
                    deleteFileCnt++;
                }
                else if (item.Value.DiffKind == DiffKind.Unchanged)
                {
                    equalFileCnt++;
                }
                else
                {
                    unknowFileCnt++;
                }
            }

            OutputMessage("File comparison summary:", true);
            OutputMessage(String.Format("{0,-30}{1,10:N0}", "Number of all files:", totalFileCnt), true);
            OutputMessage(String.Format("{0,-30}{1,10:N0}", "Number of equal files:", equalFileCnt), true);
            OutputMessage(String.Format("{0,-30}{1,10:N0}", "Number of different files:", diffFileCnt), true);
            OutputMessage(String.Format("{0,-30}{1,10:N0}", "Number of new files:", newFileCnt), true);
            OutputMessage(String.Format("{0,-30}{1,10:N0}", "Number of deleted files:", deleteFileCnt), true);

            if (unknowFileCnt > 0)
            {
                OutputMessage(String.Format("{0,-30}{1,10:N0}", "Number of unknow files:", unknowFileCnt), true);
            }
        }


        /// <summary>
        /// 檢查參數值
        /// </summary>
        /// <param name="args">命令列參數</param>
        /// <returns>true:檢驗OK, false:有誤</returns>
        private static bool CheckArgument(string[] args)
        {
            if (args == null || args.Length != 4)
            {
                ShowArgumentTips();
                return false;
            }

            if (!Directory.Exists(args[0]))
            {
                OutputMessage($"Source path not exists!", true);
                return false;
            }

            if (!Directory.Exists(args[1]))
            {
                OutputMessage($"Target path not exists!", true);
                return false;
            }

            if (!(args[3].Equals("true", StringComparison.OrdinalIgnoreCase) ||
                args[3].Equals("false", StringComparison.OrdinalIgnoreCase)))
            {
                OutputMessage($"Invalid Argument value - OutputDiffFile!", true);
                return false;
            }

            return true;
        }


        /// <summary>
        /// 輸出訊息至控制台
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <param name="writeLog">是否寫出至紀錄檔案</param>
        private static void OutputMessage(string message, bool writeLog)
        {
            Console.WriteLine(message);

            if (writeLog)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log", $"log_{DateTime.Today.ToString("yyyyMMdd")}.txt"),
                    $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff")} {message}\r\n", Encoding.UTF8);
            }
        }



        /// <summary>
        /// 顯示參數提示說明
        /// </summary>
        private static void ShowArgumentTips()
        {
            Console.WriteLine(
@"Usage: FileCompare [SourcePath] [TargetPath] [OutputResultPath] [OutputDiffFile]
Compare both folders and files, generate compare list and difference result.

Example: FileCompare ""d:\\code\\v0.6"" ""d:\\code\\v0.7"" ""d:\\result"" true

  Compare parameters
  SourcePath: Be compare source folder path, maybe the old version files folder path.
  TargetPath: Be compare target folder path, maybe the new version files folder path.
  OutputResultPath: The location of the compare results output folder path, include compare list and difference report files.
  OutputDiffFile: true or false, enable or disable output difference report files.

All rights reserved by Maya.
");
        }

    }
}
