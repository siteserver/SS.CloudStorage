using System.Collections.Generic;
using System.IO;
using Aliyun.OSS;
using Aliyun.OSS.Util;
using SiteServer.Plugin;
using SS.CloudStorage.Core;
using SS.CloudStorage.Models;

namespace SS.CloudStorage
{
    public class Main : PluginBase
    {
        private readonly Dictionary<int, ConfigInfo> _dict = new Dictionary<int, ConfigInfo>();

        public static Main Instance { get; private set; }

        public override void Startup(IService service)
        {
            Instance = this;

            var siteIds = SiteApi.GetSiteIdList();
            foreach (var siteId in siteIds)
            {
                var config = ConfigApi.GetConfig<ConfigInfo>(siteId, nameof(ConfigInfo));
                if (config == null) continue;

                _dict.Add(siteId, config);

                if (!config.IsEnabled || !config.IsInitSyncAll || string.IsNullOrEmpty(config.AccessKeyId) || string.IsNullOrEmpty(config.AccessKeySecret) || string.IsNullOrEmpty(config.BucketName) || string.IsNullOrEmpty(config.BucketEndPoint)) continue;

                var client = new OssClient(config.BucketEndPoint, config.AccessKeyId, config.AccessKeySecret);

                var summaryDict = new Dictionary<string, OssObjectSummary>();

                ObjectListing result;
                var nextMarker = string.Empty;
                do
                {
                    var listObjectsRequest = new ListObjectsRequest(config.BucketName)
                    {
                        Marker = nextMarker,
                        MaxKeys = 100
                    };
                    if (!string.IsNullOrEmpty(config.BucketPath))
                    {
                        listObjectsRequest.Prefix = config.BucketPath;
                    }
                    result = client.ListObjects(listObjectsRequest);
                    foreach (var summary in result.ObjectSummaries)
                    {
                        summaryDict.Add(summary.Key, summary);
                    }
                    nextMarker = result.NextMarker;
                } while (result.IsTruncated);

                var siteDirectoryPath = SiteApi.GetSitePath(siteId);

                List<string> GetAllFilePathList(DirectoryInfo dir)//搜索文件夹中的文件
                {
                    var fileList = new List<string>();
                    var allFile = dir.GetFiles();
                    foreach (var fi in allFile)
                    {
                        fileList.Add(fi.FullName);
                    }
                    var allDir = dir.GetDirectories();
                    foreach (var d in allDir)
                    {
                        GetAllFilePathList(d);
                    }
                    return fileList;
                }

                var allFilePathList = GetAllFilePathList(new DirectoryInfo(siteDirectoryPath));

                foreach (var filePath in allFilePathList)
                {
                    var key = (config.BucketPath + Utils.GetRelativePath(filePath, siteDirectoryPath)).Trim('/');
                    if (!summaryDict.ContainsKey(key))
                    {
                        client.PutObject(config.BucketName, key, filePath);
                    }
                    else
                    {
                        var summary = summaryDict[key];
                        using (var fs = File.Open(filePath, FileMode.Open))
                        {
                            var md5 = OssUtils.ComputeContentMd5(fs, fs.Length);
                            if (md5 != summary.ETag)
                            {
                                client.PutObject(config.BucketName, key, filePath);
                            }
                        }
                    }
                }
            }

            FileSystemWatcher watcher = new FileSystemWatcher(PhysicalApplicationPath);
            watcher.Changed += WatcherOnChanged;

            service.AddSiteMenu(siteId => new Menu
            {
                Text = "阿里云OSS",
                Href = "admin/index.html"
            });
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted) return;
            if (string.IsNullOrEmpty(Path.GetExtension(e.FullPath))) return;

            var filePath = e.FullPath;
            var siteId = SiteApi.GetSiteIdByFilePath(e.FullPath);
            if (siteId <= 0) return;
            var siteDirectoryPath = SiteApi.GetSitePath(siteId);
            if (string.IsNullOrEmpty(siteDirectoryPath)) return;

            if (!_dict.ContainsKey(siteId)) return;

            var config = _dict[siteId];

            var key = (config.BucketPath + Utils.GetRelativePath(filePath, siteDirectoryPath)).Trim('/');

            if (string.IsNullOrEmpty(key)) return;

            var client = new OssClient(config.BucketEndPoint, config.AccessKeyId, config.AccessKeySecret);
            client.PutObject(config.BucketName, key, filePath);
        }

        //public override Func<IRequestContext, string, string, object> JsonGetWithNameAndId => ActionJsonGetWithNameAndId;

        //private object ActionJsonGetWithNameAndId(IRequestContext context, string name, string id)
        //{
        //    var publishmentSystemId = context.GetQueryInt("PublishmentSystemId");
        //    return !_dict.ContainsKey(publishmentSystemId) ? null : _dict[publishmentSystemId];
        //}

        //public override Func<IRequestContext, string, string, object> JsonPostWithNameAndId => ActionJsonPostWithNameAndId;

        //private object ActionJsonPostWithNameAndId(IRequestContext context, string name, string id)
        //{
        //    var config = new Config
        //    {
        //        IsEnabled = context.GetPostBool("isEnabled"),
        //        AccessKeyId = context.GetPostString("accessKeyId"),
        //        AccessKeySecret = context.GetPostString("accessKeySecret"),
        //        BucketName = context.GetPostString("bucketName"),
        //        BucketEndPoint = context.GetPostString("bucketEndPoint"),
        //        BucketPath = context.GetPostString("bucketPath"),
        //        IsInitSyncAll = context.GetPostBool("isInitSyncAll")
        //    };

        //    var publishmentSystemId = context.GetQueryInt("PublishmentSystemId");

        //    _context.ConfigApi.SetConfig(publishmentSystemId, nameof(Config), config);
        //    _dict[publishmentSystemId] = config;

        //    return null;
        //}
    }
}