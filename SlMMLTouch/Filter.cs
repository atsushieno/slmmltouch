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
// $Id: Filter.cs 58 2009-04-04 12:56:58Z hikarin $
//

using System;

namespace SlMML
{
    public enum FilterType
    {
        LPFQuality = 1,
        LPFFast = 2,
        None = 0,
        HPFFast = -2,
        HPFQuality = -1
    };

    public class Filter
    {
        #region public methods
        public void Reset()
        {
            m_t1 = 0;
            m_t2 = 0;
            m_b0 = 0;
            m_b1 = 0;
            m_b2 = 0;
            m_b3 = 0;
            m_b4 = 0;
        }

        public void GetSample(ref double[] samples, int start, int end)
        {
            double cut = 0, k = GetKeyValue(Key), fb = 0;
            switch (Switch)
            {
                case FilterType.HPFFast:
                    for (int i = start; i < end; i++)
                    {
                        double input = 0;
                        cut = Channel.FrequencyFromIndex((int)(Frequency + Amount * Envelope.NextAmplitudeLinear)) * k;
                        UpdateValueForFastFilter(ref cut, out input, samples[i]);
                        samples[i] = input - m_b4;
                    }
                    break;
                case FilterType.HPFQuality:
                    fb = 0;
                    if (Amount > 0.0001 || Amount < -0.0001)
                    {
                        for (int i = start; i < end; i++)
                        {
                            cut = Channel.FrequencyFromIndex((int)(Frequency + Amount * Envelope.NextAmplitudeLinear)) * k;
                            UpdateCutAndFb(ref cut, ref fb);
                            UpdateSamplesForHPFQuality(ref samples, i, cut, fb);
                        }
                    }
                    else
                    {
                        cut = Channel.FrequencyFromIndex((int)(Frequency)) * k;
                        UpdateCutAndFb(ref cut, ref fb);
                        for (int i = start; i < end; i++)
                            UpdateSamplesForHPFQuality(ref samples, i, cut, fb);
                    }
                    break;
                case FilterType.LPFQuality:
                    fb = 0;
                    if (Amount > 0.0001 || Amount < -0.0001)
                    {
                        for (int i = start; i < end; i++)
                        {
                            cut = Channel.FrequencyFromIndex((int)(Frequency + Amount * Envelope.NextAmplitudeLinear)) * k;
                            UpdateCutAndFb(ref cut, ref fb);
                            UpdateSamplesForLPFQuality(ref samples, i, cut, fb);
                        }
                    }
                    else
                    {
                        cut = Channel.FrequencyFromIndex((int)(Frequency)) * k;
                        UpdateCutAndFb(ref cut, ref fb);
                        for (int i = start; i < end; i++)
                            UpdateSamplesForLPFQuality(ref samples, i, cut, fb);
                    }
                    break;
                case FilterType.LPFFast:
                    for (int i = start; i < end; i++)
                    {
                        double input = 0;
                        cut = Channel.FrequencyFromIndex((int)(Frequency + Amount * Envelope.NextAmplitudeLinear)) * k;
                        UpdateValueForFastFilter(ref cut, out input, samples[i]);
                        samples[i] = m_b4;
                    }
                    break;
            }
        }
        #endregion

        #region nonpublic methods
        private void UpdateCutValue(ref double cut)
        {
            if (cut < 1.0 / 127.0)
                cut = 0;
            cut = Math.Min(cut, 1.0 - 0.0001);
        }

        private void UpdateCutAndFb(ref double cut, ref double fb)
        {
            UpdateCutValue(ref cut);
            fb = Resonance + Resonance / (1.0 - cut);
        }

        private double GetKeyValue(double key)
        {
            return key * (2.0 * Math.PI / (Sample.RATE * Sample.FREQUENCY_BASE));
        }

        private void UpdateSamplesForHPFQuality(ref double[] samples, int index, double cut, double fb)
        {
            double input = samples[index];
            m_b0 = m_b0 + cut * (input - m_b0 + fb * (m_b0 - m_b1));
            m_b1 = m_b1 + cut * (m_b0 - m_b1);
            samples[index] = input - m_b0;
        }

        private void UpdateSamplesForLPFQuality(ref double[] samples, int index, double cut, double fb)
        {
            m_b0 = m_b0 + cut * (samples[index] - m_b0 + fb * (m_b0 - m_b1));
            samples[index] = m_b1 = m_b1 + cut * (m_b0 - m_b1);
        }

        private void UpdateValueForFastFilter(ref double cut, out double input, double sample)
        {
            UpdateCutValue(ref cut);
            double q = 1.0 - cut;
            double p = cut + 0.8 * cut * q;
            double f = p + p - 1.0;
            q = Resonance * (1.0 + 0.5 * q * (1.0 - q + 5.6 * q * q));
            input = sample;
            input -= q * m_b4;
            m_t1 = m_b1;
            m_b1 = (input + m_b0) * p - m_b1 * f;
            m_t2 = m_b2;
            m_b2 = (m_b1 + m_t1) * p - m_b2 * f;
            m_t1 = m_b3;
            m_b3 = (m_b2 + m_t2) * p - m_b3 * f;
            m_b4 = (m_b3 + m_t1) * p - m_b4 * f;
            m_b4 = m_b4 - m_b4 * m_b4 * m_b4 * 0.16667;
            m_b0 = input;
        }
        #endregion

        #region public properties
        public FilterType Switch
        {
            private get
            {
                return m_type;
            }
            set
            {
                Reset();
                m_type = value;
            }
        }

        public Envelope Envelope
        {
            private get;
            set;
        }

        public double Frequency
        {
            private get;
            set;
        }

        public double Amount
        {
            private get;
            set;
        }

        public double Resonance
        {
            private get;
            set;
        }

        public double Key
        {
            private get;
            set;
        }
        #endregion

        #region member variables
        private FilterType m_type;
        private double m_t1;
        private double m_t2;
        private double m_b0;
        private double m_b1;
        private double m_b2;
        private double m_b3;
        private double m_b4;
        #endregion
    }
}
