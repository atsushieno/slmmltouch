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
// $Id: Formant.cs 106 2009-05-16 11:21:42Z hikarin $
//

namespace SlMML
{
    public enum FormantVowel
    {
        Unknown = -1,
        A = 0,
        E,
        I,
        O,
        U
    }

    public class Formant
    {
        #region constructors and destructor
        public Formant()
        {
            m_vowel = FormantVowel.A;
            m_power = false;
            m_leftMemory = new double[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            m_rightMemory = new double[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        }
        #endregion

        #region public methods
        public void Disable()
        {
            m_power = false;
            Reset();
        }

        public void Reset()
        {
            for (int i = 0; i < 10; i++)
                m_leftMemory[i] = m_rightMemory[i] = 0;
        }

        public void GetSamples(ref double[] samples, int start, int end)
        {
            if (!m_power || m_vowel == FormantVowel.Unknown)
                return;
            double[] coeff = m_coeff[(int)m_vowel];
            for (int i = start; i < end; i++)
            {
                double sample = samples[i];
                double value = coeff[0] * sample
                    + coeff[1] * m_leftMemory[0]
                    + coeff[2] * m_leftMemory[1]
                    + coeff[3] * m_leftMemory[2]
                    + coeff[4] * m_leftMemory[3]
                    + coeff[5] * m_leftMemory[4]
                    + coeff[6] * m_leftMemory[5]
                    + coeff[7] * m_leftMemory[6]
                    + coeff[8] * m_leftMemory[7]
                    + coeff[9] * m_leftMemory[8]
                    + coeff[10] * m_leftMemory[9];
                samples[i] = value;
                m_leftMemory[9] = m_leftMemory[8];
                m_leftMemory[8] = m_leftMemory[7];
                m_leftMemory[7] = m_leftMemory[6];
                m_leftMemory[6] = m_leftMemory[5];
                m_leftMemory[5] = m_leftMemory[4];
                m_leftMemory[4] = m_leftMemory[3];
                m_leftMemory[3] = m_leftMemory[2];
                m_leftMemory[2] = m_leftMemory[1];
                m_leftMemory[1] = m_leftMemory[0];
                m_leftMemory[0] = value;
            }
        }
        #endregion

        #region public properties
        public FormantVowel Vowel
        {
            set
            {
                m_power = true;
                m_vowel = value;
            }
        }
        #endregion

        #region member variables
        private readonly double[][] m_coeff = new double[][] {
        new double[] {
            8.11044e-06, 8.943665402, -36.83889529, 92.01697887, -154.337906, 181.6233289,
            -151.8651235, 89.09614114, -35.10298511, 8.388101016, -0.923313471 
        },
        new double[] {
            4.36215e-06, 8.90438318, -36.55179099, 91.05750846, -152.422234, 179.1170248,
            -149.6496211, 87.78352223, -34.60687431, 8.282228154, -0.914150747
        },
        new double[] {
            3.33819e-06, 8.893102966, -36.49532826, 90.96543286, -152.4545478, 179.4835618,
            -150.315433, 88.43409371, -34.98612086, 8.407803364, -0.932568035
        },
        new double[] {
            1.13572e-06, 8.994734087, -37.2084849, 93.22900521, -156.6929844, 184.596544,
            -154.3755513, 90.49663749, -35.58964535, 8.478996281, -0.929252233
        },
        new double[] {
            4.09431e-07, 8.997322763, -37.20218544, 93.11385476, -156.2530937, 183.7080141,
            -153.2631681, 89.59539726, -35.12454591, 8.338655623, -0.910251753
        }
        };
        private double[] m_leftMemory;
        private double[] m_rightMemory;
        private FormantVowel m_vowel;
        private bool m_power;
        #endregion
    }
}
