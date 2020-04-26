namespace GcsTool.Services
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Google.Apis.Storage.v1.Data;
    using Google.Cloud.Storage.V1;

    /// <summary>
    /// Provides services for Google Cloud Storage using the available APIs.
    /// </summary>
    public class GoogleStorageService
    {
        #region Fields

        private readonly StorageClient _client;

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
        }

        #endregion

        #region Public Methods

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
    }
}