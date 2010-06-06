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
// $Id: Parser.cs 138 2009-08-22 13:31:42Z hikarin $
//

using System;
using System.Collections.Generic;
using System.Text;

namespace SlMML.Parsers
{
    public enum CompilerWarning
    {
        UnknownCommand,
        UnClosedRepeat,
        UnOpenedComment,
        UnClosedComment,
        RecursiveMacro
    }

    abstract class Parser
    {
        #region constants
        protected const int MULTIPLY = 32;
        protected const int MAX_PIPE = 3;
        protected const int MAX_SYNC_SOURCE = 3;
        #endregion

        #region nonpublic methods
        protected static void RemoveSpaces(ref StringBuilder s)
        {
            StringBuilder ns = new StringBuilder(s.Length);
            char[] chars = s.ToString().ToCharArray();
            foreach (char c in chars)
                if (!char.IsWhiteSpace(c))
                    ns.Append(c);
            s = ns;
        }

        protected static void ToLower(ref StringBuilder s)
        {
            s = new StringBuilder(s.ToString().ToLower());
        }
        
        protected void ParseBefore(string stringToParse)
        {
            Init(stringToParse);
            m_sequencer = new Sequencer(MULTIPLY);
            m_warnings = new List<CompilerWarning>();
            m_tracks.Add(CreateTrack());
            m_tracks.Add(CreateTrack());
        }
        
        protected Sequencer ParseAfter()
        {
            int trackCount = m_tracks.Count;
            Track track = m_tracks[trackCount - 1];
            if (track.EventCount == 0)
            {
                m_tracks.Remove(track);
                trackCount--;
            }
            track = m_tracks[Track.TEMPO_TRACK];
            track.ConductTracks(m_tracks);
            for (int i = Track.TEMPO_TRACK; i < trackCount; i++)
            {
                track = m_tracks[i];
                if (i > Track.TEMPO_TRACK)
                {
                    track.RecordRest((uint)2000);
                    track.RecordClose();
                }
                m_sequencer.AddTrack(track);
            }
            m_sequencer.CreatePipes(m_maxPipe + 1);
            m_sequencer.CreateSyncSources(m_maxSyncSource + 1);
            return m_sequencer;
        }

        protected void Init(string stringToParse)
        {
            m_tracks = new List<Track>();
            m_warnings = new List<CompilerWarning>();
            m_text = new StringBuilder(stringToParse);
            m_trackIndex = Track.FIRST_TRACK;
            m_octave = 4;
            m_relativeDir = true;
            m_velocity = 100;
            m_velocityDetail = true;
            m_length = TickFromLength(4);
            m_tempo = 120;
            m_keyOff = true;
            m_gate = 15;
            m_maxGate = 16;
            m_form = OscillatorForm.Pulse;
            m_noteShift = 0;
            m_maxPipe = 0;
        }
        
        protected void FirstLetterToken()
        {
            Track track;
            char c = CharacterTokenAndNext();
            switch (c)
            {
                case 'c':
                    NoteToken(0);
                    break;
                case 'd':
                    NoteToken(2);
                    break;
                case 'e':
                    NoteToken(4);
                    break;
                case 'f':
                    NoteToken(5);
                    break;
                case 'g':
                    NoteToken(7);
                    break;
                case 'a':
                    NoteToken(9);
                    break;
                case 'b':
                    NoteToken(11);
                    break;
                case 'r':
                    RestToken();
                    break;
                case 'o':
                    m_octave = Math.Min(Math.Max(IntToken(m_octave), -2), 8);
                    break;
                case 'v':
                    m_velocityDetail = false;
                    m_velocity = Math.Min(Math.Max(IntToken((m_velocity - 7) / 8) * 8 + 7, 0), Channel.VELOCITY_MAX2);
                    break;
                case 'l':
                    m_length = TickFromDotToken(TickFromLength((int)UIntToken(0)));
                    break;
                case '(':
                    m_velocity += m_velocityDetail ? 1 : 8;
                    m_velocity = Math.Min(m_velocity, Channel.VELOCITY_MAX2);
                    break;
                case ')':
                    m_velocity -= m_velocityDetail ? 1 : 8;
                    m_velocity = Math.Max(m_velocity, 0);
                    break;
                case 't':
                    m_tempo = Math.Max((int)UIntToken((uint)m_tempo), 1);
                    Track tempoTrack = m_tracks[Track.TEMPO_TRACK];
                    tempoTrack.RecordTempo(m_tempo, m_tracks[m_trackIndex].GlobalTick);
                    break;
                case 'q':
                    m_gate = (int)UIntToken((uint)m_gate);
                    m_tracks[m_trackIndex].RecordGate((m_gate * 1.0) / m_maxGate);
                    break;
                case '<':
                    m_octave += m_relativeDir ? 1 : -1;
                    break;
                case '>':
                    m_octave += m_relativeDir ? -1 : 1;
                    break;
                case ';':
                    track = m_tracks[m_trackIndex];
                    if (track.EventCount > 0)
                    {
                        track = CreateTrack();
                        m_tracks.Add(track);
                        ++m_trackIndex;
                    }
                    break;
                case '@':
                    AtmarkToken();
                    break;
                case 'x':
                    m_tracks[m_trackIndex].RecordVolumeMode((int)UIntToken((uint)1));
                    break;
                case 'n':
                    char c0 = CharacterToken();
                    if (c0 == 's')
                    {
                        NextToken();
                        m_noteShift = IntToken(m_noteShift);
                    }
                    else
                        // warning: unknown command
                        m_warnings.Add(CompilerWarning.UnknownCommand);
                    break;
                default:
                    if (c < 128)
                        // warning: unknown command
                        m_warnings.Add(CompilerWarning.UnknownCommand);
                    break;
            }
        }
        
        protected void AtmarkToken()
        {
            Track track;
            char c = CharacterToken();
            int tmp = 1;
            int attack = 0;
            int decay = 64;
            int sustain = 32;
            int release = 0;
            int sens = 0;
            int mode = 0;
            switch (c)
            {
                case 'v':
                    m_velocityDetail = false;
                    NextToken();
                    m_velocity = (int)UIntToken((uint)m_velocity);
                    m_velocity = Math.Min(m_velocity, Channel.VELOCITY_MAX2);
                    break;
                case 'x':
                    NextToken();
                    int expression = (int)UIntToken((uint)Channel.VELOCITY_MAX2);
                    expression = Math.Min(expression, Channel.VELOCITY_MAX2);
                    track = m_tracks[m_trackIndex];
                    track.RecordExpression(expression);
                    break;
                case 'e':
                    NextToken();
                    tmp = (int)UIntToken((uint)tmp);
                    if (CharacterToken() == ',')
                        NextToken();
                    attack = (int)UIntToken((uint)attack);
                    if (CharacterToken() == ',')
                        NextToken();
                    decay = (int)UIntToken((uint)decay);
                    if (CharacterToken() == ',')
                        NextToken();
                    sustain = (int)UIntToken((uint)sustain);
                    if (CharacterToken() == ',')
                        NextToken();
                    release = (int)UIntToken((uint)release);
                    track = m_tracks[m_trackIndex];
                    track.RecordEnvelopeADSR(attack, decay, sustain, release, tmp == 1);
                    break;
                case 'n':
                    NextToken();
                    int noise = (int)Math.Min(UIntToken(0), Channel.VELOCITY_MAX2);
                    m_tracks[m_trackIndex].RecordNoiseFrequency(noise);
                    break;
                case 'w':
                    NextToken();
                    int pwm = (int)Math.Min(Math.Max(UIntToken(50), 1), 99);
                    m_tracks[m_trackIndex].RecordPWM(pwm);
                    break;
                case 'p':
                    NextToken();
                    int pan = (int)Math.Min(Math.Max(UIntToken(64), 1), Channel.VELOCITY_MAX2);
                    m_tracks[m_trackIndex].RecordPan(pan);
                    break;
                case '\'':
                    NextToken();
                    FormantVowel vowel = FormantVowel.Unknown;
                    while (true)
                    {
                        char v = CharacterToken();
                        if (v == '\'')
                        {
                            NextToken();
                            break;
                        }
                        else
                        {
                            switch (v)
                            {
                                case 'a':
                                    vowel = FormantVowel.A;
                                    break;
                                case 'e':
                                    vowel = FormantVowel.E;
                                    break;
                                case 'i':
                                    vowel = FormantVowel.I;
                                    break;
                                case 'o':
                                    vowel = FormantVowel.O;
                                    break;
                                case 'u':
                                    vowel = FormantVowel.U;
                                    break;
                            }
                            NextToken();
                        }
                    }
                    m_tracks[m_trackIndex].RecordFormantVowel(vowel);
                    break;
                case 'd':
                    NextToken();
                    int detune = IntToken(0);
                    m_tracks[m_trackIndex].RecordDetune(detune);
                    break;
                case 'l':
                    NextToken();
                    bool reverse = false;
                    int form = 1, subform = 0, delay = 0, time = 0;
                    int depth = (int)UIntToken(0);
                    if (CharacterToken() == ',')
                        NextToken();
                    int width = (int)UIntToken(0);
                    if (CharacterToken() == ',')
                    {
                        NextToken();
                        if (CharacterToken() == '-')
                        {
                            reverse = true;
                            NextToken();
                        }
                        form = (int)UIntToken((uint)form) + 1;
                        if (CharacterToken() == '-')
                        {
                            NextToken();
                            subform = (int)UIntToken((uint)subform);
                        }
                        if (CharacterToken() == ',')
                        {
                            NextToken();
                            delay = (int)UIntToken((uint)delay);
                            if (CharacterToken() == ',')
                            {
                                NextToken();
                                time = (int)UIntToken((uint)time);
                            }
                        }
                    }
                    track = m_tracks[m_trackIndex];
                    track.RecordLFO(
                        (OscillatorForm)form,
                        (OscillatorForm)subform,
                        depth,
                        width,
                        delay,
                        time,
                        reverse
                        );
                    break;
                case 'f':
                    NextToken();
                    int amount = 0, frequency = 0, resonance = 0;
                    int swt = IntToken(0);
                    if (CharacterToken() == ',')
                    {
                        NextToken();
                        amount = IntToken(0);
                        if (CharacterToken() == ',')
                        {
                            NextToken();
                            frequency = (int)UIntToken(0);
                            if (CharacterToken() == ',')
                            {
                                NextToken();
                                resonance = (int)UIntToken(0);
                            }
                        }
                    }
                    track = m_tracks[m_trackIndex];
                    track.RecordLPF((FilterType)swt, amount, frequency, resonance);
                    break;
                case 'q':
                    NextToken();
                    int gate2 = (int)UIntToken(2);
                    m_tracks[m_trackIndex].RecordGate(gate2);
                    break;
                case 'i':
                    NextToken();
                    sens = (int)UIntToken(0);
                    if (CharacterToken() == ',')
                    {
                        NextToken();
                        attack = (int)UIntToken((uint)attack);
                        attack = Math.Min(attack, m_maxPipe);
                    }
                    m_tracks[m_trackIndex].RecordInput(sens, attack);
                    break;
                case 'o':
                    NextToken();
                    mode = (int)UIntToken(0);
                    if (CharacterToken() == ',')
                    {
                        NextToken();
                        attack = (int)UIntToken((uint)attack);
                        if (attack > m_maxPipe)
                        {
                            m_maxPipe = attack;
                            if (m_maxPipe >= MAX_PIPE)
                                m_maxPipe = attack = MAX_PIPE;
                        }
                    }
                    m_tracks[m_trackIndex].RecordOutput((ChannelOutputMode)mode, attack);
                    break;
                case 'r':
                    NextToken();
                    sens = (int)UIntToken(0);
                    if (CharacterToken() == ',')
                    {
                        NextToken();
                        attack = (int)UIntToken((uint)attack);
                        attack = Math.Min(attack, m_maxPipe);
                    }
                    m_tracks[m_trackIndex].RecordRing(sens, attack);
                    break;
                case 's':
                    NextToken();
                    mode = (int)UIntToken(0);
                    if (CharacterToken() == ',')
                    {
                        NextToken();
                        attack = (int)UIntToken((uint)attack);
                        if (mode == 1)
                        {
                            if (attack > m_maxSyncSource)
                            {
                                m_maxSyncSource = attack;
                                if (m_maxSyncSource >= MAX_SYNC_SOURCE)
                                    m_maxSyncSource = attack = MAX_SYNC_SOURCE;
                            }
                        }
                        else if (mode == 2)
                        {
                            if (attack > m_maxSyncSource)
                                attack = m_maxSyncSource;
                        }
                    }
                    m_tracks[m_trackIndex].RecordSync((ChannelOutputMode)mode, attack);
                    break;
                default:
                    m_form = (OscillatorForm)UIntToken((uint)m_form);
                    subform = 0;
                    if (CharacterToken() == '-')
                    {
                        NextToken();
                        subform = (int)UIntToken(0);
                    }
                    track = m_tracks[m_trackIndex];
                    track.RecordForm(m_form, (OscillatorForm)subform);
                    break;
            }
        }
        
        protected void NoteToken(int noteIndex)
        {
            noteIndex += m_noteShift + KeySignToken();
            int length = (int)UIntToken(0);
            int tick = TickFromDotToken(TickFromLength(length));
            bool keyOn = (m_keyOff == false) ? false : true;
            m_keyOff = true;
            if (CharacterToken() == '&')
            {
                NextToken();
                m_keyOff = false;
            }
            Track track = m_tracks[m_trackIndex];
            track.RecordNote(noteIndex + m_octave * 12, tick, m_velocity, keyOn, m_keyOff);
        }
        
        protected void RestToken()
        {
            int length = (int)UIntToken(0);
            int tick = TickFromDotToken(TickFromLength(length));
            Track track = m_tracks[m_trackIndex];
            track.RecordRest(tick);
        }

        protected int KeySignToken()
        {
            int key = 0;
            bool loop = true;
            while (loop)
            {
                char c = CharacterToken();
                switch (c)
                {
                    case '+':
                    case '#':
                        key++;
                        NextToken();
                        break;
                    case '-':
                        key--;
                        NextToken();
                        break;
                    default:
                        loop = false;
                        break;
                }
            }
            return key;
        }
        
        protected int IntToken(int defaultValue)
        {
            char c = CharacterToken();
            int sign = 1;
            switch (c)
            {
                case '+':
                    NextToken();
                    break;
                case '-':
                    sign = -1;
                    NextToken();
                    break;
            }
            return (int)(UIntToken((uint)defaultValue) * sign);
        }

        protected uint UIntToken(uint defaultValue)
        {
            long ret = 0, sum = 0;
            int index = m_index;
            while (true)
            {
                char c = CharacterToken();
                if (char.IsDigit(c))
                {
                    sum = sum * 10 + (c - '0');
                    NextToken();
                    if (sum < uint.MaxValue)
                        ret = sum;
                    else
                        break;
                }
                else
                    break;
            }
            return index == m_index ? defaultValue : (uint)ret;
        }

        protected int TickFromDotToken(int tick)
        {
            char c = CharacterToken();
            int t = tick;
            while (c == '.')
            {
                NextToken();
                t /= 2;
                tick += t;
                c = CharacterToken();
            }
            return tick;
        }

        protected int TickFromLength(int length)
        {
            return length == 0 ? m_length : 384 / length;
        }
        
        protected char CharacterToken()
        {
            if (m_index < m_text.Length)
                return m_text[m_index];
            else
                return char.MinValue;
        }
        
        protected char CharacterTokenAndNext()
        {
            char c = CharacterToken();
            m_index++;
            return c;
        }
        
        protected void NextToken()
        {
            m_index++;
        }
        
        protected Track CreateTrack()
        {
            m_octave = 4;
            m_velocity = 100;
            m_noteShift = 0;
            return new Track();
        }
#if DEBUG
        internal List<List<Dictionary<string, string>>> Dump()
        {
            List<List<Dictionary<string,string>>> r = new List<List<Dictionary<string,string>>>();
            foreach (Track track in m_tracks)
                r.Add(track.Dump());
            return r;
        }
#endif
        #endregion

        #region member variables
        protected List<CompilerWarning> m_warnings;
        private List<Track> m_tracks;
        private OscillatorForm m_form;
        private Sequencer m_sequencer;
        protected StringBuilder m_text;
        protected int m_index;
        protected bool m_relativeDir;
        protected int m_length;
        private int m_trackIndex;
        private int m_octave;
        private int m_velocity;
        private bool m_velocityDetail;
        private int m_tempo;
        private bool m_keyOff;
        private int m_gate;
        private int m_maxGate;
        private int m_noteShift;
        private int m_maxPipe;
        private int m_maxSyncSource;
        #endregion
    }
}
