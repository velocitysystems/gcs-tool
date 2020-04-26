namespace GcsTool.Models
{
    using System;

    /// <summary>
    /// DTO for a transcribed file.
    /// </summary>
    public class TranscribedFile
    {
        /// <summary>
        /// Gets or sets the audio path.
        /// </summary>
        public string AudioPath { get; set; }

        /// <summary>
        /// Gets or sets the audio URI.
        /// </summary>
        public string AudioUri { get; set; }

        /// <summary>
        /// Gets or sets the created timestamp.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the array of transcribed text blocks.
        /// </summary>
        public TranscribedTextBlock[] TextBlocks { get; set; }
    }
}
