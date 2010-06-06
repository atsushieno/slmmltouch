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
// $Id: Channel.cs 138 2009-08-22 13:31:42Z hikarin $
//

using System;
using SlMML.Modulators;

namespace SlMML
{
    public enum ChannelOutputMode
    {
        Default = 0,
        Overwrite = 1,
        Add = 2
    }

    public class Channel
    {
        #region constants
        public const int PITCH_RESOLUTION = 100;
        public const int VELOCITY_MAX = 128;
        public const int VELOCITY_MAX2 = VELOCITY_MAX - 1;
        #endregion

        #region public class methods
        public static void Initialize(int nbSamples)
        {
            if (!s_initialized)
            {
                int i = 0;
                s_sampleLength = nbSamples;
                s_samples = new double[nbSamples];
                s_frequencyLength = s_frequencyMap.Length;
                for (i = 0; i < s_frequencyLength; i++)
                    s_frequencyMap[i] = Sample.FREQUENCY_BASE * Math.Pow(2.0, (i - 69.0 * PITCH_RESOLUTION) / (12.0 * PITCH_RESOLUTION));
                s_volumeLength = VELOCITY_MAX;
                s_volumeMap[0] = 0.0;
                for (i = 1; i < s_volumeLength; i++)
                    s_volumeMap[i] = Math.Pow(10.0, (i - VELOCITY_MAX2) * (48.0 / (VELOCITY_MAX2 * 20.0)));
                s_pipeArray = null;
                s_initialized = true;
            }
        }

        public static void Release()
        {
            s_pipeArray = null;
            s_samples = null;
        }

        public static void CreatePipes(int number)
        {
            s_pipeArray = new double[number][];
            s_pipeArrayNum = number;
            for (int i = 0; i < number; i++)
            {
                s_pipeArray[i] = new double[s_sampleLength];
                for (int j = 0; j < s_sampleLength; j++)
                    s_pipeArray[i][j] = 0;
            }
        }

        public static void CreateSyncSources(int number)
        {
            s_syncSource = new bool[number][];
            s_syncSourceLength = number;
            for (int i = 0; i < number; i++)
            {
                s_syncSource[i] = new bool[s_sampleLength];
                for (int j = 0; j < s_sampleLength; j++)
                    s_syncSource[i][j] = false;
            }
        }

        public static double FrequencyFromIndex(int index)
        {
            index = Math.Min(Math.Max(index, 0), Math.Max(s_frequencyLength, 1) - 1);
            return s_frequencyMap[index];
        }
        #endregion

        #region constructors and destructor
        public Channel()
        {
            m_vco = new Envelope(0.0, 60.0 / VELOCITY_MAX2, 30.0 / VELOCITY_MAX2, 1.0 / VELOCITY_MAX2);
            m_vcf = new Envelope(0.0, 30.0 / VELOCITY_MAX2, 0.0, 1.0);
            m_osc1 = new Oscillator();
            m_mod1 = m_osc1.CurrentModulator;
            m_osc2 = new Oscillator() { Form = OscillatorForm.Sine };
            m_osc2.MakeAsLFO();
            m_mod2 = m_osc2.CurrentModulator;
            m_filter = new Filter();
            m_osc2connect = m_enableFilter = false;
            m_formant = new Formant();
            m_volumeMode = 0;
            m_expression = 0;
            m_onCounter = 0;
            m_lfoDelay = 0;
            m_lfoDepth = 0;
            m_lfoEnd = 0;
            m_lpfAmount = 0;
            m_lpfFrequency = 0;
            m_lpfResonance = 0;
            NoteIndex = 0;
            Detune = 0;
            m_frequencyIndex = 0;
            Pan = 64;
            Expression = 127;
            Velocity = 100;
            Input = new Events.Input() { Sens = 0, Pipe = 0 };
            Output = new Events.Output() { Mode = ChannelOutputMode.Default, Pipe = 0 };
            Ring = new Events.Ring() { Sens = 0, Pipe = 0 };
            Sync = new Events.Sync() { Mode = ChannelOutputMode.Default, Pipe = 0 };
        }
        #endregion

        #region public methods
        public void EnableNote(Events.NoteOn noteOn)
        {
            int index = noteOn.Index, velocity = noteOn.Velocity;
            NoteIndex = index;
            m_vco.Trigger(false);
            m_vcf.Trigger(true);
            m_mod1.ResetPhase();
            m_mod2.ResetPhase();
            m_filter.Reset();
            m_onCounter = 0;
            Velocity = velocity;
            FCNoise fcNoise = (FCNoise)m_osc1.ModulatorFromForm(OscillatorForm.FCNoise);
            fcNoise.NoiseFrequencyIndex = index;
            GBLongNoise longNoise = (GBLongNoise)m_osc1.ModulatorFromForm(OscillatorForm.GBLongNoise);
            longNoise.NoiseFrequencyIndex = index;
            GBShortNoise shortNoise = (GBShortNoise)m_osc1.ModulatorFromForm(OscillatorForm.GBShortNoise);
            shortNoise.NoiseFrequencyIndex = index;
            FCDPCM fcDPCM = (FCDPCM)m_osc1.ModulatorFromForm(OscillatorForm.FCDPCM);
            fcDPCM.DPCMFrequency = index;
        }

        public void DisableNote()
        {
            m_vco.Release();
            m_vcf.Release();
        }

        public void Close()
        {
            DisableNote();
            m_filter.Switch = FilterType.None;
        }

        public void SetLFO(Events.LFO lfo, double frequency)
        {
            OscillatorForm mainForm = lfo.Main - 1;
            m_osc2.Form = mainForm;
            m_mod2 = m_osc2.ModulatorFromForm(mainForm);
            m_osc2sign = lfo.Reverse ? -1.0 : 1.0;
            if (mainForm >= OscillatorForm.Max)
                m_osc2connect = false;
            if (mainForm == OscillatorForm.GBWave)
            {
                GBWave gbWave = (GBWave)m_osc2.ModulatorFromForm(OscillatorForm.GBWave);
                gbWave.WaveIndex = (int)lfo.Sub;
            }
            m_lfoDepth = lfo.Depth;
            m_osc2connect = m_lfoDepth == 0 ? false : true;
            m_mod2.Frequency = frequency;
            m_mod2.ResetPhase();
            Noise noise = (Noise)m_osc2.ModulatorFromForm(OscillatorForm.Noise);
            noise.NoiseFrequency = frequency / Sample.RATE;
            m_lfoDelay = lfo.Delay;
            int time = lfo.Time;
            m_lfoEnd = time > 0 ? m_lfoDelay + time : 0;
        }

        public void GetSamples(ref double[] samples, int start, int delta, int max)
        {
            int end = Math.Min(start + delta, max);
            int frequencyIndex = 0;
            if (!m_vco.Playing)
            {
                for (int i = start; i < end; i++)
                    s_samples[i] = 0;
            }
            else if (m_inSens < 0.000001)
            {
                if (!m_osc2connect)
                {
                    switch (m_syncMode)
                    {
                        case ChannelOutputMode.Overwrite:
                            m_mod1.GetSamplesSyncOut(ref s_samples, ref s_syncSource[m_syncPipe], start, end);
                            break;
                        case ChannelOutputMode.Add:
                            m_mod1.GetSamplesSyncIn(ref s_samples, s_syncSource[m_syncPipe], start, end);
                            break;
                        default:
                            m_mod1.GetSamples(ref s_samples, start, end);
                            break;
                    }
                    if (VolumeMode == 0)
                        m_vco.GetAmplitudeSamplesLinear(ref s_samples, start, end, m_ampLevel);
                    else
                        m_vco.GetAmplitudeSamplesNonLinear(ref s_samples, start, end, m_ampLevel);
                }
                else
                {
                    int s = start, e = 0;
                    do
                    {
                        e = Math.Min(s + s_lfoDelta, end);
                        frequencyIndex = m_frequencyIndex;
                        if (m_onCounter >= m_lfoDelay &&
                            (m_lfoEnd == 0 || m_onCounter < m_lfoEnd))
                        {
                            frequencyIndex += (int)(m_mod2.NextSample * m_osc2sign * m_lfoDepth);
                            m_mod2.AddPhase(e - s - 1);
                        }
                        m_mod1.Frequency = Channel.FrequencyFromIndex(frequencyIndex);
                        switch (m_syncMode)
                        {
                            case ChannelOutputMode.Overwrite:
                                m_mod1.GetSamplesSyncOut(ref s_samples, ref s_syncSource[m_syncPipe], s, e);
                                break;
                            case ChannelOutputMode.Add:
                                m_mod1.GetSamplesSyncIn(ref s_samples, s_syncSource[m_syncPipe], s, e);
                                break;
                            default:
                                m_mod1.GetSamples(ref s_samples, s, e);
                                break;
                        }
                        if (VolumeMode == 0)
                            m_vco.GetAmplitudeSamplesLinear(ref s_samples, s, e, m_ampLevel);
                        else
                            m_vco.GetAmplitudeSamplesNonLinear(ref s_samples, s, e, m_ampLevel);
                        m_onCounter += e - s;
                        s = e;
                    } while (s < end);
                }
            }
            else
            {
                if (!m_osc2connect)
                {
                    m_mod1.Frequency = Channel.FrequencyFromIndex(m_frequencyIndex);
                    for (int i = start; i < end; i++)
                        s_samples[i] = m_mod1.NextSampleFrom((int)(s_pipeArray[m_inPipe][i] * m_inSens));
                    if (m_volumeMode == 0)
                        m_vco.GetAmplitudeSamplesLinear(ref s_samples, start, end, m_ampLevel);
                    else
                        m_vco.GetAmplitudeSamplesNonLinear(ref s_samples, start, end, m_ampLevel);
                }
                else
                {
                    for (int i = start; i < end; i++)
                    {
                        frequencyIndex = m_frequencyIndex;
                        if (m_onCounter >= m_lfoDelay &&
                            (m_lfoEnd == 0 || m_onCounter < m_lfoEnd))
                            frequencyIndex += (int)(m_mod2.NextSample * m_osc2sign * m_lfoDepth);
                        m_mod1.Frequency = Channel.FrequencyFromIndex(frequencyIndex);
                        s_samples[i] = m_mod1.NextSampleFrom((int)(s_pipeArray[m_inPipe][i] * m_inSens));
                        m_onCounter++;
                    }
                    if (m_volumeMode == 0)
                        m_vco.GetAmplitudeSamplesLinear(ref s_samples, start, end, m_ampLevel);
                    else
                        m_vco.GetAmplitudeSamplesNonLinear(ref s_samples, start, end, m_ampLevel);
                }
            }
            if (m_ringSens >= 0.000001)
            {
                for (int i = start; i < end; i++)
                    s_samples[i] *= s_pipeArray[m_ringPipe][i] * m_ringSens;
            }
            double key = m_mod1.Frequency;
            m_formant.GetSamples(ref s_samples, start, end);
            m_filter.Envelope = m_vcf;
            m_filter.Frequency = m_lpfFrequency;
            m_filter.Amount = m_lpfAmount;
            m_filter.Resonance = m_lpfResonance;
            m_filter.Key = key;
            m_filter.GetSample(ref s_samples, start, end);
            switch (m_outMode)
            {
                case ChannelOutputMode.Default:
                    for (int i = start; i < end; i++)
                    {
                        int n = i << 1;
                        double amplitude = s_samples[i];
                        samples[n] += amplitude * m_panLeft;
                        samples[n + 1] += amplitude * m_panRight;
                    }
                    break;
                case ChannelOutputMode.Overwrite:
                    for (int i = start; i < end; i++)
                        s_pipeArray[m_outPipe][i] = s_samples[i];
                    break;
                case ChannelOutputMode.Add:
                    for (int i = start; i < end; i++)
                        s_pipeArray[m_outPipe][i] += s_samples[i];
                    break;
            }
        }
        #endregion

        #region nonpublic methods
        private void SetModulatorFrequency()
        {
            m_frequencyIndex = m_noteIndex * PITCH_RESOLUTION + m_detune;
            m_mod1.Frequency = FrequencyFromIndex(m_frequencyIndex);
        }
        #endregion

        #region public properties
        public Events.VCO ADSRForVCO
        {
            set
            {
                double multiply = (1.0 / VELOCITY_MAX2);
                m_vco.SetASDR(value.Attack * multiply,
                    value.Decay * multiply,
                    value.Sustain * multiply,
                    value.Release * multiply);
            }
        }

        public Events.VCF ADSRForVCF
        {
            set
            {
                double multiply = (1.0 / VELOCITY_MAX2);
                m_vcf.SetASDR(value.Attack * multiply,
                    value.Decay * multiply,
                    value.Sustain * multiply,
                    value.Release * multiply);
            }
        }

        public Events.Form Form
        {
            set
            {
                OscillatorForm main = value.Main;
                m_osc1.Form = main;
                m_mod1 = m_osc1.ModulatorFromForm(main);
                if (main == OscillatorForm.GBWave)
                {
                    GBWave gbWave = (GBWave)m_osc1.ModulatorFromForm(OscillatorForm.GBWave);
                    gbWave.WaveIndex = (int)value.Sub;
                }
                if (main == OscillatorForm.FCDPCM)
                {
                    FCDPCM fcDCPM = (FCDPCM)m_osc1.ModulatorFromForm(OscillatorForm.FCDPCM);
                    fcDCPM.WaveIndex = (int)value.Sub;
                }
            }
        }

        public Events.LPF LPF
        {
            set
            {
                FilterType sw = value.Switch;
                if (sw >= FilterType.HPFQuality &&
                    sw <= FilterType.LPFQuality &&
                    !m_enableFilter)
                {
                    m_enableFilter = true;
                    m_filter.Switch = sw;
                }
                m_lpfAmount = Math.Min(Math.Max(value.Amount, -VELOCITY_MAX2), VELOCITY_MAX2);
                m_lpfAmount *= PITCH_RESOLUTION;
                int frequencyIndex = value.Frequency;
                frequencyIndex = Math.Min(Math.Max(frequencyIndex, 0), VELOCITY_MAX2);
                m_lpfFrequency = frequencyIndex * PITCH_RESOLUTION;
                m_lpfResonance = value.Resonance * (1.0 / VELOCITY_MAX2);
                m_lpfResonance = Math.Min(Math.Max(m_lpfResonance, 0.0), 1.0);
            }
        }

        public Events.Input Input
        {
            set
            {
                m_inSens = (1 << (value.Sens - 1)) * (1.0 / 8.0) * Modulator.PHASE_LENGTH;
                m_inPipe = Math.Min(Math.Max(value.Pipe, 0), s_pipeArrayNum);
            }
        }

        public Events.Output Output
        {
            set
            {
                m_outMode = value.Mode;
                m_outPipe = Math.Min(Math.Max(value.Pipe, 0), s_pipeArrayNum);
            }
        }

        public int NoteIndex
        {
            set
            {
                m_noteIndex = value;
                SetModulatorFrequency();
            }
        }

        public int Detune
        {
            set
            {
                m_detune = value;
                SetModulatorFrequency();
            }
        }

        public double NoiseFrequency
        {
            set
            {
                Noise noise = (Noise)m_osc1.ModulatorFromForm(OscillatorForm.Noise);
                noise.NoiseFrequency = 1.0 - value * (1.0 / VELOCITY_MAX);
            }
        }

        public int PWM
        {
            set
            {
                Pulse pulse;
                if (m_osc1.Form != OscillatorForm.FCPulse)
                {
                    pulse = (Pulse)m_osc1.ModulatorFromForm(OscillatorForm.Pulse);
                    pulse.PWM = value * (1.0 / 100.0);
                }
                else
                {
                    pulse = (Pulse)m_osc1.ModulatorFromForm(OscillatorForm.FCPulse);
                    pulse.PWM = 0.125 * value;
                }
            }
        }

        public int Pan
        {
            set
            {
                m_panRight = Math.Max((value - 1) * (0.25 / 63.0), 0.0);
                m_panLeft = (2.0 * 0.25) - m_panRight;
            }
        }

        public FormantVowel FormantVowel
        {
            set
            {
                m_formant.Vowel = value;
            }
        }

        public int VolumeMode
        {
            private get;
            set;
        }

        public int Velocity
        {
            set
            {
                int velocity = Math.Min(Math.Max(value, 0), VELOCITY_MAX2);
                m_velocity = VolumeMode > 0 ? s_volumeMap[velocity] : (double)velocity / VELOCITY_MAX2;
                m_ampLevel = m_velocity * m_expression;
            }
        }

        public int Expression
        {
            set
            {
                int expression = Math.Min(Math.Max(value, 0), VELOCITY_MAX2);
                m_expression = VolumeMode > 0 ? s_volumeMap[expression] : (double)expression / VELOCITY_MAX2;
                m_ampLevel = m_velocity * m_expression;
            }
        }

        public Events.Ring Ring
        {
            set
            {
                m_ringSens = (1 << (value.Sens - 1)) / 8.0;
                m_ringPipe = Math.Min(Math.Max(value.Pipe, 0), s_pipeArrayNum);
            }
        }

        public Events.Sync Sync
        {
            set
            {
                m_syncMode = value.Mode;
                m_syncPipe = Math.Min(Math.Max(value.Pipe, 0), s_pipeArrayNum);
            }
        }
        #endregion

        #region member variables
        private static bool s_initialized = false;
        private static double[] s_frequencyMap = new double[VELOCITY_MAX * PITCH_RESOLUTION];
        private static int s_frequencyLength = 0;
        private static double[] s_volumeMap = new double[VELOCITY_MAX];
        private static int s_volumeLength = 0;
        private static double[] s_samples = null;
        private static int s_sampleLength = 0;
        private static double[][] s_pipeArray = null;
        private static int s_syncSourceLength = 0;
        private static bool[][] s_syncSource;
        private static int s_pipeArrayNum = 0;
        private static int s_lfoDelta = VELOCITY_MAX;
        private Envelope m_vco;
        private Envelope m_vcf;
        private IModulator m_mod1;
        private Oscillator m_osc1;
        private IModulator m_mod2;
        private Oscillator m_osc2;
        private int m_noteIndex;
        private int m_detune;
        private int m_frequencyIndex;
        private bool m_osc2connect;
        private double m_osc2sign;
        private Filter m_filter;
        private bool m_enableFilter;
        private Formant m_formant;
        private double m_expression;
        private double m_velocity;
        private double m_ampLevel;
        private double m_panLeft;
        private double m_panRight;
        private int m_onCounter;
        private int m_lfoDelay;
        private double m_lfoDepth;
        private int m_lfoEnd;
        private double m_lpfAmount;
        private double m_lpfFrequency;
        private double m_lpfResonance;
        private int m_volumeMode;
        private double m_inSens;
        private int m_inPipe;
        private ChannelOutputMode m_outMode;
        private int m_outPipe;
        private double m_ringSens;
        private int m_ringPipe;
        private ChannelOutputMode m_syncMode;
        private int m_syncPipe;
        #endregion
    }
}
