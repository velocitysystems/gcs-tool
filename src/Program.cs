namespace GcsTool
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CommandLine;
    using GcsTool.Services;
    using Google.Cloud.Speech.V1;
    using MoreLinq;
    using Newtonsoft.Json;
    using Serilog;
    using static Google.Cloud.Speech.V1.RecognitionConfig.Types;

    public class Program
    {
        #region Constants

        /// <summary>
        /// The application name as reported to the API.
        /// </summary>
        public const string ApplicationName = "Google Cloud Speech-to-Text Tool";

        /// <summary>
        /// The name of the bucket in Google Cloud Storage to upload media.
        /// </summary>
        public const string BucketName = "gcs-tool";

        #endregion

        #region Fields

        private readonly Options _options;
        private readonly ILogger _logger;

        private GoogleSpeechService _speechService;
        private GoogleStorageService _storageService;
        private TaglibService _taglibService;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="Program" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="logger">The logger implementation.</param>
        public Program(Options options, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Run the program service.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task RunAsync()
        {
            if (_speechService is null)
            {
                try
                {
                    _speechService = new GoogleSpeechService(_options.CredentialsPath);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to start the speech service.");
                    return;
                }
            }

            if (_storageService is null)
            {
                try
                {
                    _storageService = new GoogleStorageService(_options.CredentialsPath);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to start the storage service.");
                    return;
                }
            }

            if (_taglibService is null)
            {
                try
                {
                    _taglibService = new TaglibService();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to start the taglib service.");
                    return;
                }
            }

            if (!File.Exists(_options.AudioPath))
            {
                _logger.Error("The audio file at path {audioPath} does not exist.");
                return;
            }

            _logger.Information("Starting transcription for {audioPath}.", _options.AudioPath);

            // Retrieve audio metadata.
            var codec = _taglibService.GetAudioCodec(_options.AudioPath);
            var sampleRate = _taglibService.GetAudioSampleRate(_options.AudioPath);

            // Match audio metadata against supported formats.
            AudioEncoding encoding = default;
            switch (codec)
            {
                case var _ when codec is TagLib.Riff.WaveFormatEx:
                    encoding = AudioEncoding.Linear16;
                    break;

                case var _ when codec is TagLib.Flac.StreamHeader:
                    encoding = AudioEncoding.Flac;
                    break;

                default:
                    throw new NotImplementedException("The codec is not supported.");
            };

            // Asynchronously upload the audio.
            _logger.Information("Uploading audio to bucket {bucketName}.", BucketName);
            var objectName = $"{Guid.NewGuid()}{Path.GetExtension(_options.AudioPath)}";
            var uploadedAudio = await _storageService.UploadAsync(BucketName, objectName, _options.AudioPath);
            var uploadedAudioUri = $"gs://{BucketName}/{objectName}";
            _logger.Information("Uploaded audio to {audioUri}.", uploadedAudioUri);

            // Asynchronously transcribe the audio.
            try
            {
                IReadOnlyList<SpeechRecognitionAlternative> transcription = null;
                await foreach (var result in _speechService.LongRunningRecognizeAsync(uploadedAudioUri, encoding, sampleRate))
                {
                    if (result.Progress < 100)
                    {
                        _logger.Information("Transcription progress {progress}%.", result.Progress);
                    }

                    transcription = result.Transcription;
                }

                _logger.Information("Transcription completed.");

                // Analyze transcription by speaker.
                var sentences = new List<AnalysedSentence>();
                var wordsBySpeakerTag = transcription.SelectMany(q => q.Words).Where(q => q.SpeakerTag != 0).GroupAdjacent(q => q.SpeakerTag);
                foreach (var group in wordsBySpeakerTag)
                {
                    var sentence = new AnalysedSentence()
                    {
                        SpeakerTag = group.Key,
                        Sentence = string.Join(" ", group.Select(x => x.Word.ToString()).ToArray())
                    };

                    sentences.Add(sentence);
                }

                // Write to JSON file.
                var json = JsonConvert.SerializeObject(sentences);
                var jsonPath = _options.OutputPath ?? Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), $"Transcription-{Path.GetFileNameWithoutExtension(_options.AudioPath)}.json");
                File.WriteAllText(jsonPath, json);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "The transcription failed.");
            }

            // Asynchronously delete the uploaded audio.
            switch (await _storageService.DeleteAsync(BucketName, objectName))
            {
                case true:
                    _logger.Information("Deleted uploaded audio.");
                    break;

                case false:
                    _logger.Information("Failed to delete uploaded audio.");
                    break;
            }
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// The main entry point for the program.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static async Task Main(string[] args)
        {
            var getParserOptions = new TaskCompletionSource<(bool Parsed, Options Options)>();

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => getParserOptions.TrySetResult((true, options)))
                .WithNotParsed(options => getParserOptions.TrySetResult((false, default)));

            var result = await getParserOptions.Task;
            if (!result.Parsed)
            {
                return;
            }

            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("audit.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                await new Program(result.Options, logger).RunAsync();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unhandled exception");
            }            
        }

        #endregion

        #region Private Classes

        /// <summary>
        /// The supported command-line options.
        /// </summary>
        public class Options
        {
            [Option('c', "credentials", Required = true, HelpText = "The path to the \"credentials.json\" file.")]
            public string CredentialsPath { get; set; }

            [Option('a', "audioPath", Required = true, HelpText = "The path to the audio file to transcribe.")]
            public string AudioPath { get; set; }

            [Option('o', "outputPath", Required = false, HelpText = "The path to the output transcription JSON file.")]
            public string OutputPath { get; set; }
        }

        /// <summary>
        /// A analysed sentence for a speaker.
        /// </summary>
        private class AnalysedSentence
        {
            /// <summary>
            /// Gets or sets the speaker tag.
            /// </summary>
            public int SpeakerTag { get; set; }

            /// <summary>
            /// Gets or sets the setence.
            /// </summary>
            public string Sentence { get; set; }
        }

        #endregion
    }
}