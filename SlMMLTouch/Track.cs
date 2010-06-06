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
// $Id: Track.cs 138 2009-08-22 13:31:42Z hikarin $
//

using System;
using System.Collections.Generic;
//using System.Windows;

namespace SlMML
{
    public class Track
    {
        #region constants
        public const int TEMPO_TRACK = 0;
        public const int FIRST_TRACK = 1;
        #endregion

        #region constructors and destructor
        public Track()
        {
            End = false;
            m_channel = new Channel();
            m_events = new List<Events.Event>(32);
            m_index = 0;
            m_delta = 0;
            GlobalTick = 0;
            m_needle = 0.0;
#if true
            Duration = TimeSpan.FromMilliseconds(0);
#else
            Duration = new Duration(TimeSpan.FromMilliseconds(0));
#endif
            BPM = DEFAULT_BPM;
            RecordGate(15.0 / 16.0);
            RecordGate(0);
        }
        #endregion

        #region public methods
        public void GetSamples(ref double[] samples, int start, int end, bool update)
        {
            if (End)
                return;
            int eventCount = m_events.Count, i = start;
            while (i < end)
            {
                bool loop = false;
                double delta = 0;
                do
                {
                    loop = false;
                    if (m_index < eventCount)
                    {
                        Events.Event e = m_events[m_index];
                        delta = e.Delta * m_spt;
                        if (m_needle >= delta)
                        {
                            loop = true;
                            if (e is Events.NoteOn)
                                m_channel.EnableNote((Events.NoteOn)e);
                            else if (e is Events.NoteOff)
                                m_channel.DisableNote();
                            else if (e is Events.Note)
                                m_channel.NoteIndex = ((Events.Note)(e)).Index;
                            else if (e is Events.Tempo)
                                BPM = ((Events.Tempo)e).Value;
                            else if (e is Events.Form)
                                m_channel.Form = (Events.Form)e;
                            else if (e is Events.VCO)
                                m_channel.ADSRForVCO = (Events.VCO)e;
                            else if (e is Events.VCF)
                                m_channel.ADSRForVCF = (Events.VCF)e;
                            else if (e is Events.NoiseFrequency)
                                m_channel.NoiseFrequency = ((Events.NoiseFrequency)e).Value;
                            else if (e is Events.PWM)
                                m_channel.PWM = ((Events.PWM)e).Value;
                            else if (e is Events.Pan)
                                m_channel.Pan = ((Events.Pan)e).Value;
                            else if (e is Events.Vowel)
                                m_channel.FormantVowel = ((Events.Vowel)e).Value;
                            else if (e is Events.Detune)
                                m_channel.Detune = ((Events.Detune)e).Value;
                            else if (e is Events.LFO)
                            {
                                Events.LFO lfo = (Events.LFO)e;
                                double width = lfo.Width * m_spt;
                                lfo.Delay = (int)(lfo.Delay * m_spt);
                                lfo.Time = (int)(lfo.Time * width);
                                m_channel.SetLFO(lfo, Sample.RATE / width);
                            }
                            else if (e is Events.LPF)
                                m_channel.LPF = (Events.LPF)e;
                            else if (e is Events.VolumeMode)
                                m_channel.VolumeMode = ((Events.VolumeMode)e).Value;
                            else if (e is Events.Input)
                                m_channel.Input = (Events.Input)e;
                            else if (e is Events.Output)
                                m_channel.Output = (Events.Output)e;
                            else if (e is Events.Expression)
                                m_channel.Expression = ((Events.Expression)e).Value;
                            else if (e is Events.Ring)
                                m_channel.Ring = (Events.Ring)e;
                            else if (e is Events.Sync)
                                m_channel.Sync = (Events.Sync)e;
                            else if (e is Events.Close)
                                m_channel.Close();
                            else if (e is Events.Eot)
                                End = true;
                            m_needle -= delta;
                            m_index++;
                        }
                    }
                }
                while (loop);
                int di = 0;
                if (m_index < eventCount)
                {
                    Events.Event e = m_events[m_index];
                    delta = e.Delta * m_spt;
                    di = (int)Math.Ceiling(delta - m_needle);
                    if (i + di >= end)
                        di = end - i;
                    m_needle += di;
                    if (update)
                        m_channel.GetSamples(ref samples, i, di, end);
                    i += di;
                }
                else
                    break;
            }
        }

        public void Seek()
        {
            GlobalTick = 0;
        }

        public void Seek(int delta)
        {
            m_delta += delta;
            GlobalTick += (uint)delta;
        }
        
        public void RecordNote(int index, int length, int velocity, bool keyOn, bool keyOff)
        {
            Events.Event e;
            if (keyOn)
                e = new Events.NoteOn() { Index = index, Velocity = velocity };
            else
                e = new Events.Note() { Index = index };
            SetDeltaAndAddEvent(e);
            if (keyOff)
            {
                int gate = Math.Max((int)(length * m_gate - m_gate2), 0);
                Seek(gate);
                e = new Events.NoteOff() { Index = index, Velocity = velocity };
                SetDeltaAndAddEvent(e);
                Seek(length - gate);
            }
            else
                Seek(length);
        }
        
        public void RecordRest(int length)
        {
            Seek(length);
        }
        
        public void RecordRest(uint msec)
        {
            int length = (int)(msec * Sample.RATE / (m_spt * 1000));
            Seek(length);
        }

        public void RecordVolume(int volume)
        {
            Events.Volume e = new Events.Volume() { Value = volume };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordTempo(int tempo, uint globalTick)
        {
            Events.Tempo e = new Events.Tempo() { Value = tempo };
            SetDelta(e);
            RecordGlobalTick(globalTick, e);
        }
        
        public void RecordEOT()
        {
            Events.Eot e = new Events.Eot() { Delta = m_delta };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordGate(double gate)
        {
            m_gate = gate;
        }
        
        public void RecordGate(int gate)
        {
            m_gate2 = Math.Max(gate, 0);
        }
        
        public void RecordForm(OscillatorForm form, OscillatorForm subform)
        {
            Events.Form e = new Events.Form() { Main = form, Sub = subform };
            SetDeltaAndAddEvent(e);
        }

        public void RecordNoiseFrequency(int frequency)
        {
            Events.NoiseFrequency e = new Events.NoiseFrequency() { Value = frequency };
            SetDeltaAndAddEvent(e);
        }

        public void RecordPWM(int pwm)
        {
            Events.PWM e = new Events.PWM() { Value = pwm };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordPan(int pan)
        {
            Events.Pan e = new Events.Pan() { Value = pan };
            SetDeltaAndAddEvent(e);
        }

        public void RecordFormantVowel(FormantVowel vowel)
        {
            Events.Vowel e = new Events.Vowel() { Value = vowel };
            SetDeltaAndAddEvent(e);
        }

        public void RecordDetune(int detune)
        {
            Events.Detune e = new Events.Detune() { Value = detune };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordLFO(OscillatorForm form, OscillatorForm subform, int depth, int width, int delay, int time, bool reverse)
        {
            Events.LFO e = new Events.LFO()
            {
                Main = form,
                Sub = subform,
                Depth = depth,
                Width = width,
                Delay = delay,
                Time = time,
                Reverse = reverse
            };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordLPF(FilterType sw, int amount, int frequency, int resonance)
        {
            Events.LPF e = new Events.LPF()
            {
                Switch = sw,
                Amount = amount,
                Frequency = frequency,
                Resonance = resonance
            };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordVolumeMode(int mode)
        {
            Events.VolumeMode e = new Events.VolumeMode() { Value = mode };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordInput(int inSens, int pipe)
        {
            Events.Input e = new Events.Input() { Sens = inSens, Pipe = pipe };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordOutput(ChannelOutputMode mode, int pipe)
        {
            Events.Output e = new Events.Output() { Mode = mode, Pipe = pipe };
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordExpression(int expression)
        {
            Events.Expression e = new Events.Expression() { Value = expression };
            SetDeltaAndAddEvent(e);
        }

        public void RecordRing(int inSens, int pipe)
        {
            Events.Ring e = new Events.Ring() { Sens = inSens, Pipe = pipe };
            SetDeltaAndAddEvent(e);
        }

        public void RecordSync(ChannelOutputMode mode, int pipe)
        {
            Events.Sync e = new Events.Sync() { Mode = mode, Pipe = pipe };
            SetDeltaAndAddEvent(e);
        }

        public void RecordClose()
        {
            Events.Close e = new Events.Close();
            SetDeltaAndAddEvent(e);
        }
        
        public void RecordEnvelopeADSR(int attack, int decay, int sustain, int release, bool isVCO)
        {
            Events.Event e;
            if (isVCO)
                e = new Events.VCO()
                {
                    Delta = m_delta,
                    Attack = attack,
                    Decay = decay,
                    Sustain = sustain,
                    Release = release
                };
            else
                e = new Events.VCF()
                {
                    Delta = m_delta,
                    Attack = attack,
                    Decay = decay,
                    Sustain = sustain,
                    Release = release
                };
            SetDeltaAndAddEvent(e);
        }

        public void ConductTracks(IList<Track> tracks)
        {
            int ni = m_events.Count;
            int nj = tracks.Count;
            uint globalSample = 0, globalTick = 0;
            double spt = CalculateSPT(DEFAULT_BPM);
            for (int i = 0; i < ni; i++)
            {
                Events.Event e = m_events[i];
                uint delta = (uint)e.Delta;
                globalTick += delta;
                globalSample += (uint)(delta * spt);
                Events.Tempo tempo = e as Events.Tempo;
                if (tempo != null)
                {
                    int tempoValue = tempo.Value;
                    for (int j = FIRST_TRACK; j < nj; j++)
                    {
                        Track track = tracks[j];
                        track.RecordTempo(tempoValue, globalTick);
                        spt = CalculateSPT(tempoValue);
                    }
                }
            }
            uint maxGlobalTick = 0;
            for (int j = FIRST_TRACK; j < nj; j++)
            {
                Track track = tracks[j];
                uint trackGlobalTick = track.GlobalTick;
                if (maxGlobalTick < trackGlobalTick)
                    maxGlobalTick = trackGlobalTick;
            }
            Events.Close close = new Events.Close();
            RecordGlobalTick(maxGlobalTick, close);
            globalSample += (uint)((maxGlobalTick - globalTick) * spt);
            RecordRest((uint)3000);
            RecordEOT();
            globalSample += (uint)(3 * Sample.RATE);
#if true
            Duration = TimeSpan.FromMilliseconds(globalSample * (1000.0 / Sample.RATE));
#else
            Duration = new Duration(TimeSpan.FromMilliseconds(globalSample * (1000.0 / Sample.RATE)));
#endif
        }
        #endregion

        #region nonpublic methods
        private void RecordGlobalTick(uint globalTick, Events.Event e)
        {
            int eventCount = m_events.Count;
            uint preGlobalTick = 0;
            for (int i = 0; i < eventCount; i++)
            {
                Events.Event ev = m_events[i];
                uint nextTick = (uint)(preGlobalTick + ev.Delta);
                if (nextTick >= globalTick)
                {
                    ev.Delta = (int)(nextTick - globalTick);
                    e.Delta = (int)(globalTick - preGlobalTick);
                    m_events.Insert(i, e);
                    return;
                }
                preGlobalTick = nextTick;
            }
            e.Delta = (int)(globalTick - preGlobalTick);
            AddEvent(e);
        }

        private void SetDeltaAndAddEvent(Events.Event e)
        {
            SetDelta(e);
            AddEvent(e);
        }

        private void SetDelta(Events.Event e)
        {
            e.Delta = m_delta;
            m_delta = 0;
        }

        private void AddEvent(Events.Event e)
        {
            m_events.Add(e);
        }

        private double CalculateSPT(double bpm)
        {
            return Sample.RATE / (bpm * 96.0 / 60.0);
        }
#if DEBUG
        internal List<Dictionary<string, string>> Dump()
        {
            List<Dictionary<string, string>> r = new List<Dictionary<string, string>>();
            foreach (Events.Event e in m_events)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data["status"] = e.GetType().Name;
                if (e is Events.NoteOn)
                {
                    Events.NoteOn noteOn = (Events.NoteOn)e;
                    data["index"] = noteOn.Index.ToString();
                    data["velocity"] = noteOn.Velocity.ToString();
                }
                else if (e is Events.Note)
                    data["index"] = ((Events.Note)(e)).Index.ToString();
                else if (e is Events.Tempo)
                    data["bpm"] = ((Events.Tempo)e).Value.ToString();
                else if (e is Events.Form)
                {
                    Events.Form form = (Events.Form)e;
                    data["form"] = ((int)form.Main).ToString();
                    data["subform"] = ((int)form.Sub).ToString();
                }
                else if (e is Events.VCO)
                {
                    Events.VCO vco = (Events.VCO)e;
                    data["attack"] = vco.Attack.ToString();
                    data["decay"] = vco.Decay.ToString();
                    r.Add(data);
                    data = new Dictionary<string, string>();
                    data["status"] = "VCO";
                    data["sustain"] = vco.Sustain.ToString();
                    data["release"] = vco.Release.ToString();
                }
                else if (e is Events.VCF)
                {
                    Events.VCF vcf = (Events.VCF)e;
                    data["attack"] = vcf.Attack.ToString();
                    data["decay"] = vcf.Decay.ToString();
                    r.Add(data);
                    data = new Dictionary<string, string>();
                    data["status"] = "VCF";
                    data["sustain"] = vcf.Sustain.ToString();
                    data["release"] = vcf.Release.ToString();
                }
                else if (e is Events.NoiseFrequency)
                    data["frequency"] = ((Events.NoiseFrequency)e).Value.ToString();
                else if (e is Events.PWM)
                    data["pwm"] = ((Events.PWM)e).Value.ToString();
                else if (e is Events.Pan)
                    data["pan"] = ((Events.Pan)e).Value.ToString();
                else if (e is Events.Vowel)
                    data["formant"] = ((int)((Events.Vowel)e).Value).ToString();
                else if (e is Events.Detune)
                    data["detune"] = ((Events.Detune)e).Value.ToString();
                else if (e is Events.LFO)
                {
                    Events.LFO lfo = (Events.LFO)e;
                    int rv = lfo.Reverse ? -1 : 1;
                    data["form"] = ((int)lfo.Main * rv).ToString();
                    data["subform"] = ((int)lfo.Sub).ToString();
                    r.Add(data);
                    double width = lfo.Width * m_spt;
                    data = new Dictionary<string, string>();
                    data["status"] = "LFO";
                    data["depth"] = lfo.Depth.ToString();
                    //data["width"] = (Sample.RATE / width).ToString();
                    r.Add(data);
                    data = new Dictionary<string, string>();
                    data["status"] = "LFO";
                    data["delay"] = ((lfo.Delay * m_spt)).ToString();
                    //data["time"] = ((lfo.Time * lfo.Width)).ToString();
                }
                else if (e is Events.LPF)
                {
                    Events.LPF lpf = (Events.LPF)e;
                    data["switch"] = ((int)lpf.Switch).ToString();
                    data["amount"] = lpf.Amount.ToString();
                    r.Add(data);
                    data = new Dictionary<string, string>();
                    data["status"] = "LPF";
                    data["frequency"] = lpf.Frequency.ToString();
                    data["resonance"] = lpf.Resonance.ToString();
                }
                else if (e is Events.VolumeMode)
                    data["volumeMode"] = ((Events.VolumeMode)e).Value.ToString();
                else if (e is Events.Input)
                {
                    Events.Input input = (Events.Input)e;
                    data["sens"] = input.Sens.ToString();
                    data["pipe"] = input.Pipe.ToString();
                }
                else if (e is Events.Output)
                {
                    Events.Output output = (Events.Output)e;
                    data["mode"] = ((int)output.Mode).ToString();
                    data["pipe"] = output.Pipe.ToString();
                }
                else if (e is Events.Expression)
                    data["expression"] = ((Events.Expression)e).Value.ToString();
                else if (e is Events.Ring)
                {
                    Events.Ring ring = (Events.Ring)e;
                    data["sens"] = ring.Sens.ToString();
                    data["pipe"] = ring.Pipe.ToString();
                }
                else if (e is Events.Output)
                {
                    Events.Sync sync = (Events.Sync)e;
                    data["mode"] = ((int)sync.Mode).ToString();
                    data["pipe"] = sync.Pipe.ToString();
                }
                r.Add(data);
            }
            return r;
        }
#endif
        #endregion

        #region public properties
        public bool End
        {
            get;
            private set;
        }

        public uint GlobalTick
        {
            get;
            private set;
        }

#if true
        public TimeSpan Duration
        {
            get;
            private set;
        }
#else
        public Duration Duration
        {
            get;
            private set;
        }
#endif
        
        public double BPM
        {
            get
            {
                return m_bpm;
            }
            set
            {
                m_bpm = value;
                m_spt = CalculateSPT(value);
            }
        }

        public int EventCount
        {
            get
            {
                return m_events.Count;
            }
        }

        public Dictionary<int,int> DeltasToSeek
        {
            get
            {
                Dictionary<int, int> r = new Dictionary<int, int>();
                int delta = 0, i = 0;
                foreach (Events.Event e in m_events)
                {
                    r[delta] = i;
                    delta += e.Delta;
                    i++;
                }
                return r;
            }
        }
        #endregion

        #region member variables
        public static readonly int DEFAULT_BPM = 120;
        private Channel m_channel;
        private List<Events.Event> m_events;
        private int m_index;
        private int m_delta;
        private double m_bpm;
        private double m_spt;
        private double m_needle;
        private double m_gate;
        private double m_gate2;
        #endregion
    }
}
