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
// $Id: Noise.cs 138 2009-08-22 13:31:42Z hikarin $
//

using System;

namespace SlMML.Modulators
{
    sealed class Noise : Modulator, IModulator
    {
        #region constants
        private const int NOISE_TABLE_MASK = TABLE_LENGTH - 1;
        private const int NOISE_PHASE_SHIFT = 30;
        private const int NOISE_PHASE_MASK = ((1 << NOISE_PHASE_SHIFT) - 1);
        #endregion

        #region nonpublic class methods
        private static void Initialize()
        {
            if (!s_initialized)
            {
                Random random = new Random();
                for (int i = 0; i < TABLE_LENGTH; i++)
                    s_table[i] = random.NextDouble() * 2.0 - 1.0;
                s_initialized = true;
            }
        }
        #endregion

        #region constructors and destructor
        public Noise() : base()
        {
            Initialize();
            NoiseFrequency = 1.0;
            ShouldResetPhase = true;
            m_counter = 0;
        }
        #endregion

        #region public methods
        public double NextSampleFrom(int offset)
        {
            double value = s_table[(m_phase + (offset << PHASE_SHIFT)) & NOISE_TABLE_MASK];
            AddPhase(1);
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

        public new void ResetPhase()
        {
            if (ShouldResetPhase)
                m_phase = 0;
        }

        public new void AddPhase(int time)
        {
            m_counter = (uint)(m_counter + m_frequencyShift * time);
            m_phase = (int)((m_phase + (m_counter >> NOISE_PHASE_SHIFT)) & NOISE_TABLE_MASK);
            m_counter &= NOISE_PHASE_MASK;
        }

        public void RestoreFrequency()
        {
            m_frequencyShift = (int)m_noiseFrequency;
        }
        #endregion

        #region public properties
        public double NextSample
        {
            get
            {
                double val = s_table[m_phase];
                AddPhase(1);
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
                m_frequency = value;
            }
        }

        public double NoiseFrequency
        {
            set
            {
                m_noiseFrequency = value * (1 << NOISE_PHASE_SHIFT);
                m_frequencyShift = (int)m_noiseFrequency;
            }
        }

        public bool ShouldResetPhase
        {
            private get;
            set;
        }
        #endregion

        #region member variables
        private static bool s_initialized = false;
        private static double[] s_table = new double[TABLE_LENGTH];
        private double m_noiseFrequency;
        private uint m_counter;
        #endregion
    }
}
