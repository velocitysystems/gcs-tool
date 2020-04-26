namespace GcsTool.Models
{
    /// <summary>
    /// DTO for a block of transcribed text by speaker.
    /// </summary>
    public class TranscribedTextBlock
    {
        /// <summary>
        /// Gets or sets the speaker tag.
        /// </summary>
        public int SpeakerTag { get; set; }

        /// <summary>
        /// Gets or sets the transcribed text.
        /// </summary>
        public string Text { get; set; }
    }
}
