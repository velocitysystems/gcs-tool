namespace GcsTool.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using Google.Cloud.Speech.V1;
    using static Google.Cloud.Speech.V1.RecognitionConfig.Types;

    /// <summary>
    /// Provides services for Google Speech-to-Text using the available APIs.
    /// </summary>
    public class GoogleSpeechService
    {
        #region Fields

        private readonly SpeechClient _client;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleSpeechService" /> class.
        /// </summary>
        /// <param name="credentialsPath">The credentials path.</param>
        public GoogleSpeechService(string credentialsPath)
        {
            var builder = new SpeechClientBuilder()
            {
                CredentialsPath = credentialsPath,
            };

            _client = builder.Build();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Asynchronously recognize and transcribe a long audio file.
        /// <para>
        /// See <a href="https://developers.google.com/drive/api/v3/search-files">Search for files and folders</a>
        /// and <a href="https://developers.google.com/drive/api/v3/about-files">Files and folders overview</a>.
        /// </para>
        /// </summary>
        /// <param name="storageUri">The storage URI for the audio.</param>
        /// <param name="encoding">Optional audio encoding type.</param>
        /// <param name="sampleRateHertz">Optional audio sample rate in hertz.</param>
        /// <param name="languageCode">Optional language code of the audio i.e. "en".</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> where each iterator returns a progress and transcription results object.</returns>
        public async IAsyncEnumerable<(int Progress, IReadOnlyList<SpeechRecognitionAlternative> Transcription)> LongRunningRecognizeAsync(
            string storageUri, 
            AudioEncoding encoding = AudioEncoding.Flac, 
            int sampleRateHertz = 44100,
            string languageCode = "en-NZ")
        {
            var config = new RecognitionConfig()
            {
                Encoding = encoding,
                SampleRateHertz = sampleRateHertz,
                LanguageCode = languageCode,
                DiarizationConfig = new SpeakerDiarizationConfig()
                {
                    EnableSpeakerDiarization = true,
                }
            };

            var longOperation = _client.LongRunningRecognize(config, RecognitionAudio.FromStorageUri(storageUri));
            var lastProgressPercent = 0;
            while (true)
            {
                if (longOperation != null && longOperation.IsCompleted)
                {
                    var response = longOperation.Result;
                    var wordAlternatives = response.Results.SelectMany(q => q.Alternatives).Where(q => q.Words.Count > 0);
                    

                    yield return (longOperation.Metadata.ProgressPercent, wordAlternatives.ToList());
                    yield break;
                }

                longOperation = await longOperation.PollOnceAsync();
                var progressPercent = longOperation.Metadata.ProgressPercent;
                if (progressPercent != lastProgressPercent)
                {
                    // Only emit progress percent if it has changed.
                    lastProgressPercent = progressPercent;
                    yield return (longOperation.Metadata.ProgressPercent, null);
                }                
            }            
        }

        #endregion
    }
}