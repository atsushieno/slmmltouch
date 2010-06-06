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
// $Id: FCDPCM.cs 138 2009-08-22 13:31:42Z hikarin $
//

using System;

namespace SlMML.Modulators
{
    sealed class FCDPCM : Modulator, IModulator
    {
        #region constants
        private const int MAX_WAVE = 16;
        private const int FC_CPU_CYCLE = 1789773;
        private const int FC_DPCM_PHASE_SHIFT = 2;
        private const int FC_DPCM_MAX_LENGTH = 0xff1;
        private const int FC_DPCM_TABLE_MAX_LENGTH = (FC_DPCM_MAX_LENGTH >> 2) + 2;
        private const int FC_DPCM_NEXT = Sample.RATE << FC_DPCM_PHASE_SHIFT;
        #endregion

        #region public class methods
        public static void SetWave(int index, int intVol, int loopFlag, string wave)
        {
            index = Math.Min(Math.Max(index, 0), MAX_WAVE - 1);
            s_intVol[index] = Math.Min(Math.Max(intVol, 0), 127);
            s_loopFlag[index] = Math.Min(Math.Max(loopFlag, 0), 1);
            s_length[index] = 0;
            int pos = 0, count = 0, count2 = 0;
            foreach (char c in wave)
            {
                int code = c;
                if (0x41 <= c && c <= 0x5a)
                    code -= 0x41;
                else if (0x61 <= c && c <= 0x7a)
                    code -= 0x61 - 26;
                else if (0x30 <= c && c <= 0x39)
                    code -= 0x30 - 26 - 26;
                else if (code == 0x2b)
                    code = 26 + 26 + 10;
                else if (code == 0x2f)
                    code = 26 + 26 + 10 + 1;
                else if (code == 0x3d)
                    code = 0;
                else
                    code = 0;
                for (int i = 5; i >= 0; i--)
                {
                    s_table[index, pos] += (uint)((code >> i) & 1) << (count * 8 + 8 - count2);
                    count2++;
                    if (count2 >= 8)
                    {
                        count2 = 0;
                        count++;
                    }
                    s_length[index]++;
                    if (count >= 4)
                    {
                        count = 0;
                        pos++;
                        if (pos >= FC_DPCM_TABLE_MAX_LENGTH)
                            pos = FC_DPCM_TABLE_MAX_LENGTH - 1;
                    }
                }
            }
            int length = s_length[index];
            length -= ((length - 8) % 0x80);
            length = Math.Min(length, FC_DPCM_MAX_LENGTH * 8);
            if (length == 0)
                length = 8;
            s_length[index] = length;
        }
        #endregion

        #region nonpublic class methods
        private static void Initialize()
        {
            if (!s_initialized)
            {
                SetWave(0, 127, 0, "");
                s_initialized = true;
            }
        }
        #endregion

        #region constructors and destructor
        public FCDPCM()
        {
            Initialize();
            ResetPhase();
            Frequency = Sample.FREQUENCY_BASE;
            WaveIndex = 0;
        }
        #endregion

        #region public methods
        public new void ResetPhase()
        {
            m_phase = 0;
            m_address = 0;
            m_bit = 0;
            m_offset = 0;
            m_wave = s_intVol[m_index];
            m_length = s_length[m_index];
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

        public double NextSampleFrom(int offset)
        {
            double value = (m_wave - 64) / 64.0;
            m_phase = (m_phase + m_frequencyShift + ((offset - m_offset) >> (PHASE_SHIFT - 7))) & PHASE_MASK;
            GetSample(ref value);
            m_offset = offset;
            return value;
        }
        #endregion

        #region nonpublic methods
        private void GetSample(ref double value)
        {
            while (FC_DPCM_NEXT <= m_phase)
            {
                m_phase -= FC_DPCM_NEXT;
                if (m_length > 0)
                {
                    if (((s_table[m_index, m_address] >> m_bit) & 1) == 1)
                    {
                        if (m_wave < 126)
                            m_wave += 2;
                    }
                    else
                        if (m_wave > 1)
                            m_wave -= 2;
                    m_bit++;
                    if (m_bit >= 32)
                    {
                        m_bit = 0;
                        m_address++;
                    }
                    m_length--;
                    if (m_length == 0)
                    {
                        if (s_loopFlag[m_index] > 0)
                        {
                            m_address = 0;
                            m_bit = 0;
                            m_length = s_length[m_index];
                        }
                    }
                }
                value = (m_wave - 64) / 64.0;
            }
        }
        #endregion

        #region public properties
        public double NextSample
        {
            get
            {
                double value = (m_wave - 64) / 64.0;
                AddPhase(1);
                GetSample(ref value);
                return value;
            }
        }

        public int WaveIndex
        {
            set
            {
                m_index = Math.Min(value, MAX_WAVE - 1);
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
                m_frequencyShift = (int)(value * (1 << (FC_DPCM_PHASE_SHIFT + 4)));
            }
        }

        public int DPCMFrequency
        {
            set
            {
                value = Math.Min(Math.Max(value, 0), MAX_WAVE - 1);
                m_frequencyShift = (FC_CPU_CYCLE << FC_DPCM_PHASE_SHIFT) / s_interval[value];
            }
        }
        #endregion

        #region member variables
        private static bool s_initialized = false;
        private static uint[,] s_table = new uint[MAX_WAVE, FC_DPCM_TABLE_MAX_LENGTH];
        private static int[] s_intVol = new int[MAX_WAVE];
        private static int[] s_loopFlag = new int[MAX_WAVE];
        private static int[] s_length = new int[MAX_WAVE];
        private static readonly int[] s_interval = new int[] {
            428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106,  85,  72,  54
        };
        private int m_address;
        private int m_bit;
        private int m_wave;
        private int m_length;
        private int m_offset;
        private int m_index;
        #endregion
    }
}
