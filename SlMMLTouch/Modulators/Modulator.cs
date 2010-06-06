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
// $Id: Modulator.cs 106 2009-05-16 11:21:42Z hikarin $
//

namespace SlMML.Modulators
{
    abstract class Modulator
    {
        #region constants
        public const int PHASE_LENGTH = TABLE_LENGTH << PHASE_SHIFT;
        protected const int TABLE_LENGTH = 1 << 16;
        protected const int PHASE_SHIFT = 14;
        protected const int PHASE_HALF = TABLE_LENGTH << (PHASE_SHIFT - 1);
        protected const int PHASE_MASK = PHASE_LENGTH - 1;
        #endregion

        #region constructors and destructor
        protected Modulator()
        {
            ResetPhase();
            Frequency = Sample.FREQUENCY_BASE;
        }
        #endregion

        #region public methods
        public void AddPhase(int time)
        {
            m_phase = (m_phase + m_frequencyShift * time) & PHASE_MASK;
        }

        public void ResetPhase()
        {
            m_phase = 0;
        }

        public double Frequency
        {
            get
            {
                return m_frequency;
            }
            set
            {
                m_frequency = value;
                m_frequencyShift = (int)(value * (PHASE_LENGTH / Sample.RATE));
            }
        }
        #endregion

        #region member variables
        protected double m_frequency;
        protected int m_frequencyShift;
        protected int m_phase;
        #endregion
    }
}
