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
// $Id: FCNoise.cs 138 2009-08-22 13:31:42Z hikarin $
//

using System;

namespace SlMML.Modulators
{
    sealed class FCNoise : Modulator, IModulator
    {
        #region constants
        private const int FC_NOISE_PHASE_SHIFT = 10;
        private const int FC_NOISE_PHASE_SECOND = 1789773 << FC_NOISE_PHASE_SHIFT;
        private const int FC_NOISE_PHASE_DELTA = FC_NOISE_PHASE_SECOND / Sample.RATE;
        #endregion

        #region constructors and destructor
        public FCNoise() : base()
        {
            SetLongMode();
            m_fcr = 0x8000;
            m_value = NoiseValue;
            NoiseFrequencyIndex = 0;
        }
        #endregion

        #region public methods
        public double NextSampleFrom(int offset)
        {
            int fcr = m_fcr, phase = m_phase, delta = FC_NOISE_PHASE_DELTA + offset;
            double value = m_value;
            NextSampleFromDelta(delta);
            if (m_phase >= m_frequencyShift)
            {
                m_phase = m_frequencyShift;
                m_value = NoiseValue;
            }
            m_fcr = fcr;
            m_phase = phase;
            double d = NextSample;
            return value;
        }

        public void GetSamples(ref double[] samples, int start, int end)
        {
            for (int i = start; i < end; i++)
                samples[i] = NextSample;
        }

        public void GetSamplesSyncIn(ref double[] samples, bool[] syncIn, int start, int end)
        {
            GetSamples(ref samples, start, end);
        }

        public void GetSamplesSyncOut(ref double[] samples, ref bool[] syncOut, int start, int end)
        {
            GetSamples(ref samples, start, end);
        }

        public new void AddPhase(int time)
        {
            m_phase = m_phase + FC_NOISE_PHASE_DELTA * time;
            while (m_phase >= m_frequencyShift)
            {
                m_phase -= m_frequencyShift;
                m_value = NoiseValue;
            }
        }

        public new void ResetPhase()
        {
            return;
        }

        public void SetShortMode()
        {
            m_snz = 6;
        }

        public void SetLongMode()
        {
            m_snz = 1;
        }
        #endregion

        #region nonpublic methods
        private void NextSampleFromDelta(int delta)
        {
            double sum = 0, count = 0;
            while (delta >= m_frequencyShift)
            {
                delta -= m_frequencyShift;
                m_phase = 0;
                sum += NoiseValue;
                count += 1.0;
            }
            if (count > 0)
                m_value = sum / count;
            m_phase += delta;
        }
        #endregion

        #region public properties
        public double NextSample
        {
            get
            {
                double val = m_value;
                int delta = FC_NOISE_PHASE_DELTA;
                NextSampleFromDelta(delta);
                if (m_phase >= m_frequencyShift)
                {
                    m_phase -= m_frequencyShift;
                    m_value = NoiseValue;
                }
                return val;
            }
        }

        public new double Frequency
        {
            get
            {
                return m_frequency;
            }
            set
            {
                m_frequency = (int)(FC_NOISE_PHASE_SECOND / value);
            }
        }

        public int NoiseFrequencyIndex
        {
            set
            {
                int index = Math.Min(Math.Max(value, 0), 15);
                m_frequencyShift = (int)s_interval[index] << FC_NOISE_PHASE_SHIFT;
            }
        }

        public double NoiseValue
        {
            get
            {
                m_fcr >>= 1;
                m_fcr |= ((m_fcr ^ (m_fcr >> m_snz)) & 1) << 15;
                return (m_fcr & 1) == 1 ? 1.0 : -1.0;
            }
        }
        #endregion

        #region member variables
        private static double[] s_interval = new double[16] {
            0x004, 0x008, 0x010, 0x020, 0x040, 0x060, 0x080, 0x0a0,
            0x0ca, 0x0fe, 0x17c, 0x1fc, 0x2fa, 0x3f8, 0x7f2, 0xfe4
        };
        private int m_fcr;
        private int m_snz;
        private double m_value;
        #endregion
    }
}
