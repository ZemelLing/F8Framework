using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace F8Framework.Core.Editor
{
    public class ABBuildTool : ScriptableObject
    {
        private static Dictionary<string, AssetBundleMap.AssetMapping> _assetMapping;
        private static Dictionary<string, string> _resourceMapping;
        /// <summary>
        /// AssetBundle名与资产文件名不同时查找
        /// </summary>
        private static Dictionary<string, string> _discrepantAssetPathMapping;

        /// <summary>
        /// 打包后AB名加上MD5（微信小游戏使用）
        /// </summary>
        private static bool _appendHashToAssetBundleName = false;

        public static void BuildAllAB()
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();

            // 获取“StreamingAssets”文件夹路径（不一定这个文件夹，可自定义）            
            string strABOutPAthDir = URLSetting.GetAssetBundlesOutPath();

            GenerateAssetNames();
            GenerateResourceNames();
            LogF8.LogAsset("自动设置AssetBundleName（AB名为空时）");
            AssetDatabase.Refresh();

            FileTools.CheckDirAndCreateWhenNeeded(strABOutPAthDir);
            AssetDatabase.Refresh();

            Caching.ClearCache();

            // 打包生成AB包 (目标平台自动根据当前平台设置，WebGL不可使用BuildAssetBundleOptions.None压缩)
            BuildPipeline.BuildAssetBundles(strABOutPAthDir, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
            LogF8.LogAsset("打包AssetBundle：" + URLSetting.GetAssetBundlesOutPath() + "  当前打包平台：" + EditorUserBuildSettings.activeBuildTarget);

            AssetDatabase.Refresh();

            // 清理多余文件夹和ab
            DeleteRemovedAssetBundles();

            // 等待AB打包完成，再写入数据
            GenerateAssetNames(true);
            GenerateResourceNames(true);
            LogF8.LogAsset("写入资产数据 生成：AssetBundleMap.json，生成：ResourceMap.json");

            AssetDatabase.Refresh();

            LogF8.LogAsset("资产打包成功!");
        }

        public static void GenerateAssetNames(bool isWrite = false)
        {
            _discrepantAssetPathMapping = isWrite ? null : new Dictionary<string, string>();
            var assetBundlesFolderPath = URLSetting.GetAssetBundlesFolder();

            FileTools.CheckDirAndCreateWhenNeeded(assetBundlesFolderPath);
            if (Directory.Exists(assetBundlesFolderPath))
            {
                // 获取文件夹的路径
                string[] folderPaths = Directory.GetDirectories(assetBundlesFolderPath, "*", SearchOption.AllDirectories);
                // 获取文件的路径
                string[] filePaths = Directory.GetFiles(assetBundlesFolderPath, "*", SearchOption.AllDirectories);
                // 合并文件夹和文件的路径，可以根据需要调整顺序
                string[] allPaths = filePaths.Concat(folderPaths).ToArray();

                List<string> tempNames = new();

                _assetMapping = new Dictionary<string, AssetBundleMap.AssetMapping>();

                foreach (string path in allPaths)
                {
                    // 排除.meta文件 .DS_Store文件
                    if (Path.GetExtension(path) == ".meta" || Path.GetExtension(path) == ".DS_Store")
                    {
                        continue;
                    }
                    string filePath = FileTools.FormatToUnityPath(path);

                    // 获取不带扩展名的文件名
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                    // 获取GetAssetPath
                    string assetPath = GetAssetPath(filePath);

                    if (File.Exists(filePath)) // 文件
                    {
                        string abName = SetAssetBundleName(assetPath);

                        if (!isWrite)
                        {
                            continue;
                        }

                        if (tempNames.Contains(fileNameWithoutExtension.ToLower()))
                        {
                            LogF8.LogError("AssetName重复，请检查资源地址（大小写不敏感）：" + filePath);
                            string id = Guid.NewGuid().ToString(); // 生成一个唯一的ID
                            fileNameWithoutExtension += id;
                        }
                        tempNames.Add(fileNameWithoutExtension.ToLower());

                        // 只留下一个assetPath
                        List<string> assetPathsForAbName = new List<string>();
                        assetPathsForAbName.Add(assetPath.ToLower());

                        _assetMapping.Add(fileNameWithoutExtension, new AssetBundleMap.AssetMapping(abName.ToLower(), assetPathsForAbName.ToArray(),
                            BuildPkgTool.ToVersion, FileTools.GetFileSize(AssetBundleHelper.GetAssetBundleFullName(abName.ToLower())).ToString(),
                            FileTools.CreateMd5ForFile(AssetBundleHelper.GetAssetBundleFullName(abName.ToLower())), GetPackage(filePath), ""));
                    }
                    else if (Directory.Exists(filePath)) // 文件夹
                    {
                        if (!isWrite)
                        {
                            continue;
                        }

                        // 文件夹资产信息，使用资产名名代替
                        string[] assetNameDir = Directory.GetFiles(filePath, "*", SearchOption.TopDirectoryOnly)
                            .Where(path => !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".DS_Store", StringComparison.OrdinalIgnoreCase))
                            .Select(path => Path.GetFileNameWithoutExtension(path))
                            .ToArray();

                        if (tempNames.Contains(fileNameWithoutExtension))
                        {
                            LogF8.LogError("AssetName重复，请检查文件夹地址（大小写不敏感）：" + filePath);
                            string id = Guid.NewGuid().ToString(); // 生成一个唯一的ID
                            fileNameWithoutExtension += id;
                        }
                        tempNames.Add(fileNameWithoutExtension);

                        _assetMapping.Add(fileNameWithoutExtension, new AssetBundleMap.AssetMapping("", assetNameDir,
                            BuildPkgTool.ToVersion, "", "", "", ""));
                    }
                }

                if (isWrite)
                {
                    // 把总的manifest加上
                    if (tempNames.Contains(URLSetting.GetPlatformName()))
                    {
                        LogF8.LogError("总AssetBundleManifest和其他资产名重复，请检查资产：" + URLSetting.GetPlatformName());
                    }
                    else
                    {
                        _assetMapping.Add(URLSetting.GetPlatformName(), new AssetBundleMap.AssetMapping(URLSetting.GetPlatformName(), new string[] { },
                            BuildPkgTool.ToVersion, FileTools.GetFileSize(AssetBundleHelper.GetAssetBundleFullName(URLSetting.GetPlatformName())).ToString(),
                            FileTools.CreateMd5ForFile(AssetBundleHelper.GetAssetBundleFullName(URLSetting.GetPlatformName())), "", ""));
                    }


                    WriteAssetNames();
                }
            }
        }

        /// <summary>
        /// 设置资源AB名字
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string SetAssetBundleName(string path)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(path);
            // 使用 Path.ChangeExtension 去掉扩展名
            string bundleName;
            if (_appendHashToAssetBundleName)
            {
                bundleName = $"{Path.ChangeExtension(path, null).Replace(URLSetting.AssetBundlesPath, "")}_{FileTools.CreateMd5ForFile(path)}";
            }
            else
            {
                bundleName = Path.ChangeExtension(path, null).Replace(URLSetting.AssetBundlesPath, "");
            }

            if (!assetImporter.assetBundleName.Equals(bundleName))
            {
                if (assetImporter.assetBundleName.IsNullOrEmpty())
                {
                    assetImporter.assetBundleName = bundleName;
                    EditorUtility.SetDirty(assetImporter);
                }
                else if (_discrepantAssetPathMapping != null)
                {
                    // 资产名和ab包名不相等
                    _discrepantAssetPathMapping["/" + assetImporter.assetBundleName] = "/" + bundleName.ToLower();
                }

            }
            return assetImporter.assetBundleName;
        }


        public static void DeleteRemovedAssetBundles()
        {
            FileTools.CheckDirAndCreateWhenNeeded(URLSetting.GetAssetBundlesFolder());
            List<string> assetPaths = new List<string>();
            string assetBundlesPath = URLSetting.GetAssetBundlesFolder();
            RecordAssetsAndDirectories(assetBundlesPath, assetBundlesPath, assetPaths, true);
            assetPaths.Add("/" + URLSetting.GetPlatformName().ToLower());

            FileTools.CheckDirAndCreateWhenNeeded(URLSetting.GetAssetBundlesOutPath());
            List<string> abPaths = new();
            string abBundlesPath = URLSetting.GetAssetBundlesOutPath();
            RecordAssetsAndDirectories(abBundlesPath, abBundlesPath, abPaths);

            foreach (string ab in abPaths)
            {
                if (!assetPaths.Contains(ab) && !AssetPathsContainsDiscrepantAssetBundle(assetPaths, ab))
                {
                    string abpath = URLSetting.GetAssetBundlesOutPath() + ab;
                    if (File.Exists(abpath))
                    {
                        // It's a file, delete the file
                        if (FileTools.SafeDeleteFile(abpath))
                        {
                            FileTools.SafeDeleteFile(abpath + ".meta");
                        }

                        if (FileTools.SafeDeleteFile(abpath + ".manifest"))
                        {
                            FileTools.SafeDeleteFile(abpath + ".manifest" + ".meta");
                        }
                        LogF8.LogAsset("删除多余AB文件：" + abpath);
                    }
                    else if (Directory.Exists(abpath))
                    {
                        // It's a folder, delete the folder
                        if (FileTools.SafeDeleteDir(abpath))
                        {
                            LogF8.LogAsset("删除多余AB文件夹：" + abpath);
                            // If the folder is deleted successfully, handle .meta file
                            string metaFilePath = abpath + ".meta";
                            if (File.Exists(metaFilePath))
                            {
                                FileTools.SafeDeleteFile(metaFilePath);
                            }
                        }
                    }
                    else
                    {
                        LogF8.LogAsset("AB文件路径不存在，已经被删除了：" + abpath);
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        public static void RecordAssetsAndDirectories(string basePath, string rootPath, List<string> assetPaths, bool removeExtension = false)
        {
            Stack<string> stack = new Stack<string>();
            stack.Push(rootPath);

            while (stack.Count > 0)
            {
                string currentPath = stack.Pop();
                string relativePath = currentPath.Replace(basePath, "");

                // Check for directories
                string[] directories = Directory.GetDirectories(currentPath);
                foreach (string directory in directories)
                {
                    stack.Push(directory);
                    assetPaths.Add(FileTools.FormatToUnityPath(directory.Replace(basePath, "").ToLower()));
                }

                // Check for files
                string[] files = Directory.GetFiles(currentPath);
                foreach (string file in files)
                {
                    string extension = Path.GetExtension(file).ToLower();
                    if (extension != ".meta" && extension != ".manifest" && extension != ".ds_store")
                    {
                        // It's a file under AssetBundles, record as "Audio/click11"
                        if (removeExtension)
                        {
                            if (_appendHashToAssetBundleName)
                            {
                                assetPaths.Add(FileTools.FormatToUnityPath(Path.ChangeExtension(relativePath + "/" + Path.GetFileName(file), null).ToLower()) + "_" + FileTools.CreateMd5ForFile(file));
                            }
                            else
                            {
                                assetPaths.Add(FileTools.FormatToUnityPath(Path.ChangeExtension(relativePath + "/" + Path.GetFileName(file), null).ToLower()));
                            }
                        }
                        else
                        {
                            assetPaths.Add(FileTools.FormatToUnityPath(relativePath + "/" + Path.GetFileName(file)).ToLower());
                        }
                    }
                }
            }
        }

        private static bool AssetPathsContainsDiscrepantAssetBundle(List<string> assetPaths, string ab)
        {
            if (_discrepantAssetPathMapping.TryGetValue(ab, out string disPath))
                return assetPaths.Contains(disPath);
            return false;
        }

        private static void WriteAssetNames()
        {
            string assetMapPath = FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) + "/AssetMap/Resources/" + nameof(AssetBundleMap) + ".json";
            FileTools.SafeDeleteFile(assetMapPath);
            FileTools.SafeDeleteFile(assetMapPath + ".meta");
            FileTools.CheckFileAndCreateDirWhenNeeded(assetMapPath);
            AssetDatabase.Refresh();

            string AssetBundleMapPath = Application.dataPath + "/F8Framework/AssetMap/Resources/" + nameof(AssetBundleMap) + ".json";
            FileTools.CheckFileAndCreateDirWhenNeeded(AssetBundleMapPath);
            FileTools.SafeWriteAllText(AssetBundleMapPath, Util.LitJson.ToJson(_assetMapping));
            AssetDatabase.Refresh();
        }

        public static void GenerateResourceNames(bool isWrite = false)
        {
            if (!isWrite)
            {
                return;
            }

            string[] dics = Directory.GetDirectories(Application.dataPath, "Resources", SearchOption.AllDirectories);
            List<string> tempNames = new List<string>();
            _resourceMapping = new Dictionary<string, string>();
            foreach (string dic in dics)
            {
                if (!Directory.Exists(dic))
                    continue;

                string[] files = Directory.GetFiles(dic, "*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    string filePath = FileTools.FormatToUnityPath(file);
                    if (!File.Exists(filePath))
                        continue;

                    if (filePath.EndsWith(".meta") ||
                        filePath.EndsWith(".DS_Store"))
                        continue;

                    // 获取不带扩展名的文件名
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);

                    string notSuffix = Path.ChangeExtension(file, null);

                    string resourcesPath = GetResourcesPath(notSuffix);

                    string realPath = resourcesPath.Replace(URLSetting.ResourcesPath, "");

                    if (tempNames.Contains(fileNameWithoutExtension))
                    {
                        LogF8.LogError("ResourceName重复，请检查资源：" + filePath);
                        string id = Guid.NewGuid().ToString(); // 生成一个唯一的ID
                        fileNameWithoutExtension += id;
                    }
                    tempNames.Add(fileNameWithoutExtension);

                    _resourceMapping.Add(fileNameWithoutExtension, realPath);
                }
            }

            WriteResourceNames();
        }

        private static void WriteResourceNames()
        {
            string resourceMapPath = FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) + "/AssetMap/Resources/" + nameof(ResourceMap) + ".json";
            FileTools.SafeDeleteFile(resourceMapPath);
            FileTools.SafeDeleteFile(resourceMapPath + ".meta");
            FileTools.CheckFileAndCreateDirWhenNeeded(resourceMapPath);
            AssetDatabase.Refresh();

            string ResourceMapPath = Application.dataPath + "/F8Framework/AssetMap/Resources/" + nameof(ResourceMap) + ".json";
            FileTools.CheckFileAndCreateDirWhenNeeded(ResourceMapPath);
            FileTools.SafeWriteAllText(ResourceMapPath, Util.LitJson.ToJson(_resourceMapping));
            AssetDatabase.Refresh();
        }

        private static string GetPackage(string path)
        {
            // 使用正则表达式切割地址
            string[] packages = Regex.Split(path, @"[\\/]");

            foreach (var package in packages)
            {
                // 判断地址中是否包含"Package_"
                int index = package.IndexOf(HotUpdateManager.PackageSplit);
                if (index != -1)
                {
                    // 如果包含，则获取"Package_"后面的所有数据
                    string part = package.Substring(index + HotUpdateManager.PackageSplit.Length);
                    return part;
                }
            }

            return "";
        }

        #region 获取路径

        public static string GetAssetBundlesPath(string fullPath)
        {
            Regex rgx = new Regex(@"AssetBundles[\\/].+$");
            Match matches = rgx.Match(fullPath);

            string assetPath = "";
            if (matches.Success)
                assetPath = matches.Value;

            assetPath = FileTools.FormatToUnityPath(assetPath);
            return assetPath;
        }

        public static string GetAssetPath(string fullPath)
        {
            Regex rgx = new Regex(@"Assets[\\/].+$");
            Match matches = rgx.Match(fullPath);

            string assetPath = "";
            if (matches.Success)
                assetPath = matches.Value;

            assetPath = FileTools.FormatToUnityPath(assetPath);
            return assetPath;
        }

        public static string GetResourcesPath(string fullPath)
        {
            Regex rgx = new Regex(@"Resources[\\/].+$");
            Match matches = rgx.Match(fullPath);

            string assetPath = "";
            if (matches.Success)
                assetPath = matches.Value;

            assetPath = FileTools.FormatToUnityPath(assetPath);
            return assetPath;
        }

        private static string GetScriptPath()
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(CreateInstance<ABBuildTool>());

            // 获取脚本在 Assets 中的相对路径
            string scriptRelativePath = AssetDatabase.GetAssetPath(monoScript);

            // 获取绝对路径并规范化
            string scriptPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scriptRelativePath));

            return scriptPath;
        }

        #endregion
    }
}
