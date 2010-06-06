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
// $Id: Pulse.cs 138 2009-08-22 13:31:42Z hikarin $
//

namespace SlMML.Modulators
{
    sealed class Pulse : Modulator, IModulator
    {
        #region constructors and destructor
        public Pulse() : base()
        {
            PWM = 0.5;
        }
        #endregion

        #region public methods
        public double NextSampleFrom(int offset)
        {
            double value = ((m_phase + offset) & PHASE_MASK) < m_pwm ? 1.0 : -1.0;
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
            for (int i = start; i < end; i++)
            {
                if (syncIn[i])
                    ResetPhase();
                samples[i] = m_phase < m_pwm ? 1.0 : -1.0;
                AddPhase(1);
            }
        }

        public void GetSamplesSyncOut(ref double[] samples, ref bool[] syncOut, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                samples[i] = m_phase < m_pwm ? 1.0 : -1.0;
                m_phase += m_frequencyShift;
                syncOut[i] = m_phase > PHASE_MASK;
                m_phase &= PHASE_MASK;
            }
        }
        #endregion

        #region public properties
        public double NextSample
        {
            get
            {
                double val = m_phase < m_pwm ? 1.0 : -1.0;
                AddPhase(1);
                return val;
            }
        }

        public double PWM
        {
            set
            {
                m_pwm = value * PHASE_LENGTH;
            }
        }
        #endregion

        #region member variables
        private double m_pwm;
        #endregion
    }
}
