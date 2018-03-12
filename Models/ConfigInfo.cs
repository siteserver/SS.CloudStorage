namespace SS.CloudStorage.Models
{
    public class ConfigInfo
    {
        public bool IsEnabled { get; set; }
        public string AccessKeyId { get; set; }
        public string AccessKeySecret { get; set; }
        public string BucketName { get; set; }
        public string BucketEndPoint { get; set; }
        public string BucketPath { get; set; }
        public bool IsInitSyncAll { get; set; }
    }
}
