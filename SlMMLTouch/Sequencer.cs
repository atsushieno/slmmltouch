/*
 Copyright (c) 2009, hkrn All rights reserved.
 
 Redistribution and use in source and binary forms, with or without
 modification, are permitted provided that the following conditions are met:
 
 Redistributions of source code must retain the above copyright notice, this
 list of conditions and the following disclaimer. Redistributions in binary
 form must reproduce the above copyright notice, this list of conditions and
 the following disclaimer in the documentation and/or other materials
 provided with the distribution. Neither the name of the hkrn nor
 the names of its contributors may be used to endorse or promote products
 derived from this software without specific prior written permission. 
 
 THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR
 ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
 DAMAGE.
 */

//
// $Id: Sequencer.cs 138 2009-08-22 13:31:42Z hikarin $
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
//using System.Windows.Media;

namespace SlMML
{
    enum SequencerStep
    {
        None,
        Pre,
        Track,
        Post
    }

    public sealed class Sequencer //: MediaStreamSource
    {
        #region constants
        public const int BUFFER_SIZE = 8192;
        public const int BUFFER_ARRAY_SIZE = 2;
        private const int BLOCK_ALIGNMENT = 2 * sizeof(short);
        private const int BUFFER_BLOCK_SIZE = BUFFER_SIZE * BLOCK_ALIGNMENT;
        private const string WAVE_HEADER = "0100020044ac000010b10200040010000000";
        #endregion

        #region constructors and destructor
        public Sequencer(int multiple)
        {
            m_bufferSize = BUFFER_SIZE * multiple;
            m_buffer = new double[m_bufferSize << 1];
            m_tracks = new List<Track>();
            m_multiple = multiple;
#if false
            m_mediaSampleAttributes = new Dictionary<MediaSampleAttributeKeys, string>();
#endif
            MasterVolume = 100;
            Channel.Initialize(BUFFER_SIZE * multiple);
        }
        #endregion

#if true

        public ArraySegment<byte> ProcessNextSamples ()
        {
			ArraySegment<byte> ret;
            if (m_endCount > 1)
            {
                ret = default (ArraySegment<byte>);
            }
            else
            {
                using (MemoryStream stream = new MemoryStream(BUFFER_BLOCK_SIZE))
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    int index = m_count * BUFFER_SIZE;
                    double volume = Math.Min(MasterVolume, 100) / 100;
                    for (int i = index; i < index + BUFFER_SIZE; i++)
                    {
                        int bufferIndex = i << 1;
                        writer.Write((m_buffer[bufferIndex] * volume).ToShort());
                        writer.Write((m_buffer[bufferIndex + 1] * volume).ToShort());
                    }
                    m_count++;
                    m_timestampIndex += m_timestampBlock;
                    ret = new ArraySegment<byte> (stream.GetBuffer (), 0, (int) stream.Length);
                }
            }
            if (m_count == m_multiple)
            {
                GetSamples();
                m_count = 0;
                if ((m_tracks[Track.TEMPO_TRACK]).End)
                    m_endCount++;
            }
			return ret;
        }
#else
        #region MediaStreamSource overrides
        protected override void OpenMediaAsync()
        {
            m_timestampBlock = (long)Math.Round((decimal)(TimeSpan.FromSeconds(1).Ticks / (BUFFER_BLOCK_SIZE * m_multiple)));
            Dictionary<MediaStreamAttributeKeys, string> mediaStreamAttributes = new Dictionary<MediaStreamAttributeKeys, string>();
            mediaStreamAttributes[MediaStreamAttributeKeys.CodecPrivateData] = WAVE_HEADER;
            description = new MediaStreamDescription(MediaStreamType.Audio, mediaStreamAttributes);
            Dictionary<MediaSourceAttributesKeys, string> mediaSourceAttributes = new Dictionary<MediaSourceAttributesKeys, string>();
            mediaSourceAttributes[MediaSourceAttributesKeys.CanSeek] = false.ToString();
            mediaSourceAttributes[MediaSourceAttributesKeys.Duration] = m_tracks[Track.TEMPO_TRACK].Duration.TimeSpan.Ticks.ToString(CultureInfo.InvariantCulture);
            List<MediaStreamDescription> availableMediaStreams = new List<MediaStreamDescription>();
            availableMediaStreams.Add(new MediaStreamDescription(MediaStreamType.Audio, mediaStreamAttributes));
            ReportOpenMediaCompleted(mediaSourceAttributes, availableMediaStreams);
            ReportGetSampleProgress(0);
            GetSamples();
        }

        protected override void CloseMedia()
        {
        }

        protected override void GetDiagnosticAsync(MediaStreamSourceDiagnosticKind diagnosticKind)
        {
            ReportGetDiagnosticCompleted(diagnosticKind, 0);
        }

        protected override void GetSampleAsync(MediaStreamType mediaStreamType)
        {
            MediaStreamSample sample;
            if (m_endCount > 1)
            {
                sample = new MediaStreamSample(description, null, 0, 0, 0, m_mediaSampleAttributes);
                ReportGetSampleCompleted(sample);
            }
            else
            {
                using (MemoryStream stream = new MemoryStream(BUFFER_BLOCK_SIZE))
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    int index = m_count * BUFFER_SIZE;
                    double volume = Math.Min(MasterVolume, 100) / 100;
                    for (int i = index; i < index + BUFFER_SIZE; i++)
                    {
                        int bufferIndex = i << 1;
                        writer.Write((m_buffer[bufferIndex] * volume).ToShort());
                        writer.Write((m_buffer[bufferIndex + 1] * volume).ToShort());
                    }
                    m_count++;
                    m_timestampIndex += m_timestampBlock;
                    sample = new MediaStreamSample(description, stream, 0, BUFFER_BLOCK_SIZE, m_timestampIndex, m_mediaSampleAttributes);
                    ReportGetSampleCompleted(sample);
                }
            }
            if (m_count == m_multiple)
            {
                GetSamples();
                m_count = 0;
                if ((m_tracks[Track.TEMPO_TRACK]).End)
                    m_endCount++;
            }
        }

        protected override void SeekAsync(long seekToTime)
        {
            ReportSeekCompleted(seekToTime);
        }

        protected override void SwitchMediaStreamAsync(MediaStreamDescription mediaStreamDescription)
        {
            ReportSwitchMediaStreamCompleted(mediaStreamDescription);
        }
        #endregion
#endif

        #region public methods
        public void ClearTracks()
        {
            m_tracks.Clear();
        }
        
        public void AddTrack(Track track)
        {
            m_tracks.Add(track);
        }

        public void CreatePipes(int number)
        {
            Channel.CreatePipes(number);
        }

        public void CreateSyncSources(int number)
        {
            Channel.CreateSyncSources(number);
        }
        #endregion

        #region nonpublic methods
        private void GetSamples()
        {
            bool loop = true;
            int blen = Math.Min((BUFFER_SIZE << 2), m_bufferSize);
            int offset = 0;
            int trackIndex = 0;
            int trackCount = m_tracks.Count;
            SequencerStep step = SequencerStep.Pre;
            while (loop)
            {
                switch (step)
                {
                    case SequencerStep.Pre:
                        for (int i = (m_bufferSize << 1) - 1; i >= 0; i--)
                            m_buffer[i] = (0.0).ToShort();
                        if (trackCount > 0)
                        {
                            Track track = m_tracks[Track.TEMPO_TRACK];
                            track.GetSamples(ref m_buffer, 0, m_bufferSize, false);
                        }
                        step = SequencerStep.Track;
                        trackIndex = Track.FIRST_TRACK;
                        offset = 0;
                        break;
                    case SequencerStep.Track:
                        if (trackIndex >= trackCount)
                            step = SequencerStep.Post;
                        else
                        {
                            Track track = m_tracks[trackIndex];
                            track.GetSamples(ref m_buffer, offset, offset + blen, true);
                            offset += blen;
                            if (offset >= m_bufferSize)
                            {
                                offset = 0;
                                trackIndex++;
                                //m_buffered = (m_trackIndex + 1.0) / (trackCount + 1.0);
                            }
                        }
                        break;
                    case SequencerStep.Post:
                        loop = false;
                        break;
                }
            }
        }
        #endregion

        #region public properties
        public uint MasterVolume
        {
            get;
            set;
        }
        #endregion

        #region member variables
#if false
        private Dictionary<MediaSampleAttributeKeys, string> m_mediaSampleAttributes;
        private MediaStreamDescription description;
#endif
        private List<Track> m_tracks;
        private double[] m_buffer;
        private long m_timestampIndex;
        private long m_timestampBlock;
        private int m_bufferSize;
        private int m_multiple;
        private int m_count;
        private int m_endCount;
        #endregion
    }
}
