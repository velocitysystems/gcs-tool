using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TagLib;

namespace GcsTool.Services
{
    /// <summary>
    /// Provides services for reading media metadata.
    /// </summary>
    public class TaglibService
    {
        #region Public Methods

        /// <summary>
        /// Get the audio codec for the media.
        /// </summary>
        /// <param name="audioPath">The audio path.</param>
        /// <returns>The audio codec for th emedia..</returns>
        public ICodec GetAudioCodec(string audioPath)
        {
            var file = TagLib.File.Create(audioPath);
            return file.Properties.Codecs?.FirstOrDefault();
        }

        /// <summary>
        /// Get the audio sample rate in hertz.
        /// </summary>
        /// <param name="audioPath">The audio path.</param>
        /// <returns>The sample rate in hertz.</returns>
        public int GetAudioSampleRate(string audioPath)
        {
            var file = TagLib.File.Create(audioPath);
            return file.Properties.AudioSampleRate;
        }


        #endregion
    }
}
