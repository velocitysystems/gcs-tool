namespace GcsTool
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CommandLine;
    using GcsTool.Services;
    using Google.Cloud.Speech.V1;
    using Serilog;

    public class Program
    {
        #region Constants

        /// <summary>
        /// The application name as reported to the API.
        /// </summary>
        public const string ApplicationName = "Google Cloud Speech-to-Text Tool";

        #endregion

        #region Fields

        private readonly Options _options;
        private readonly ILogger _logger;
        private GoogleSpeechService _service;

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
            if (_service is null)
            {
                try
                {
                    _service = new GoogleSpeechService(_options.CredentialsPath);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to start the service.");
                    return;
                }
            }

            if (!File.Exists(_options.AudioPath))
            {
                _logger.Error("The audio path {path} does not exist or may have been deleted.", _options.AudioPath);
                return;
            }

            _logger.Information("Starting transcription for {audioPath}.", _options.AudioPath);

            // Asynchronously transcribe the audio.
            IReadOnlyList<SpeechRecognitionAlternative> transcription = null;
            await foreach (var result in _service.LongRunningRecognizeAsync(_options.AudioPath))
            {
                if (result.Progress < 100)
                {
                    _logger.Information("Transcription progress {progress}%.", result.Progress);
                    continue;
                }

                transcription = result.Transcription;
                _logger.Information("Transcription completed.");
            }

            // TODO: Process the audio.
            var t = transcription;
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

            [Option('a', "audio", Required = true, HelpText = "The path to the audio file to transcribe.")]
            public string AudioPath { get; set; }
        }

        #endregion
    }
}