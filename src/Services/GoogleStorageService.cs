namespace GcsTool.Services
{
    using System.IO;
    using System.Threading.Tasks;
    using Google.Apis.Storage.v1.Data;
    using Google.Cloud.Storage.V1;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides services for Google Cloud Storage using the available APIs.
    /// </summary>
    public class GoogleStorageService
    {
        #region Fields

        private readonly StorageClient _client;
        private readonly string _projectId;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleStorageService" /> class.
        /// </summary>
        /// <param name="credentialsPath">The credentials path.</param>
        public GoogleStorageService(string credentialsPath)
        {
            var builder = new StorageClientBuilder()
            {
                CredentialsPath = credentialsPath,
            };

            _client = builder.Build();
            _projectId = GetProjectId(credentialsPath);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Asychronously get the bucket.
        /// </summary>
        /// <param name="bucket">The bucket name.</param>
        /// <returns>The bucket or null if does not exist.</returns>
        public async Task<Bucket> GetBucketAsync(string bucket)
        {
            try
            {
                return await _client.GetBucketAsync(bucket);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Asychronously create the bucket.
        /// </summary>
        /// <param name="bucket">The bucket name.</param>
        /// <returns>The created bucket or null if does not exist.</returns>
        public async Task<Bucket> CreateBucketAsync(string bucket)
        {
            try
            {
                return await _client.CreateBucketAsync(_projectId, bucket);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Asychronously upload the file to the bucket.
        /// </summary>
        /// <param name="bucket">The bucket name.</param>
        /// <param name="objectName">The object name.</param>
        /// <param name="filePath">The path to the file to upload.</param>
        /// <returns>The created object or null if failed to upload.</returns>
        public async Task<Object> UploadAsync(string bucket, string objectName, string filePath)
        {
            try
            {
                using var file = File.OpenRead(filePath);
                return await _client.UploadObjectAsync(bucket, objectName, null, file);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Asychronously delete the object from the bucket.
        /// </summary>
        /// <param name="bucket">The bucket name.</param>
        /// <param name="objectName">The object name.</param>
        /// <returns>True if deleted, else false.</returns>
        public async Task<bool> DeleteAsync(string bucket, string objectName)
        {
            try
            {
                await _client.DeleteObjectAsync(bucket, objectName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get the project identifier from the credentials.
        /// </summary>
        /// <param name="credentialsPath">The credentials path.</param>
        /// <returns>The project identifier.</returns>
        private string GetProjectId(string credentialsPath)
        {
            var json = File.ReadAllText(credentialsPath);
            var o = JObject.Parse(json);

            return (string)o["project_id"];
        }

        #endregion
    }
}