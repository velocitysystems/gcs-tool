namespace GcsTool.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using Google.Cloud.Speech.V1;
    using Google.LongRunning;
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
        /// <param name="audioPath">The audio path.</param>
        /// <param name="encoding">Optional audio encoding type.</param>
        /// <param name="sampleRateHertz">Optional audio sample rate in hertz.</param>
        /// <param name="languageCode">Optional language code of the audio i.e. "en".</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}" /> where each iterator returns a progress, completed and results object.</returns>
        public async IAsyncEnumerable<(int Progress, bool Completed, IReadOnlyList<SpeechRecognitionResult> Results)> LongRunningRecognizeAsync(
            string audioPath, 
            AudioEncoding encoding = AudioEncoding.Linear16, 
            int sampleRateHertz = 44100,
            string languageCode = "en")
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

            var longOperation = _client.LongRunningRecognize(config, RecognitionAudio.FromFile(audioPath));
            while (true)
            {
                if (longOperation != null && longOperation.IsCompleted)
                {
                    var response = longOperation.Result;
                    var results = response.Results;

                    yield return (100, true, results.ToList());
                    yield break;
                }

                longOperation = await longOperation.PollOnceAsync();
                yield return (longOperation.Metadata.ProgressPercent, false, null);
            }            
        }

        #endregion
    }
}