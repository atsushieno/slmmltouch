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
// $Id: Envelope.cs 58 2009-04-04 12:56:58Z hikarin $
//

using System;

namespace SlMML
{
    public class Envelope
    {
        #region nonpublic class methods
        private static void Initialize()
        {
            if (!s_initialized)
            {
                s_volumeMap[0] = 0.0;
                for (int i = 0; i < s_volumeLength; i++)
                    s_volumeMap[i] = Math.Pow(10.0, (i - 255.0) * (48.0 / (255.0 * 20.0)));
                s_initialized = true;
            }
        }
        #endregion

        #region constructors and destructor
        public Envelope(double attack, double decay, double sustain, double release)
        {
            Initialize();
            SetASDR(attack, decay, sustain, release);
            Playing = false;
            m_releasing = true;
            m_currentValue = 0;
            m_releaseStep = 0;
        }
        #endregion

        #region public methods
        public void SetASDR(double attack, double decay, double sustain, double release)
        {
            if (attack != 0.0)
            {
                m_attackTime = (int)(attack * Sample.RATE);
                m_attackRcpr = 1.0 / m_attackTime;
            }
            if (decay != 0.0)
            {
                m_decayTime = (int)(decay * Sample.RATE);
                m_decayRcpr = 1.0 / m_decayTime;
            }
            m_sustainLevel = sustain;
            m_releaseTime = (release > 0.0 ? release : (1.0 / Channel.VELOCITY_MAX2)) * Sample.RATE;
        }

        public void Trigger(bool zeroStart)
        {
            Playing = true;
            m_releasing = false;
            m_startAmplitude = zeroStart ? 0 : m_currentValue;
            m_timeInSamples = 1;
        }

        public void Release()
        {
            m_releasing = true;
            m_releaseStep = m_currentValue / m_releaseTime;
        }

        public void GetAmplitudeSamplesLinear(ref double[] samples, int start, int end, double velocity)
        {
            for (int i = start; i < end; i++)
            {
                if (!Playing)
                {
                    samples[i] = 0.0;
                    continue;
                }
                double n = samples[i];
                UpdateCurrentValue();
                samples[i] = n * m_currentValue * velocity;
            }
        }

        public void GetAmplitudeSamplesNonLinear(ref double[] samples, int start, int end, double velocity)
        {
            for (int i = start; i < end; i++)
            {
                if (!Playing)
                {
                    samples[i] = (0.0).ToShort();
                    continue;
                }
                double n = samples[i];
                UpdateCurrentValue();
                samples[i] = n * s_volumeMap[(int)(Math.Min(m_currentValue, 1.0) * 255)] * velocity;
            }
        }
        #endregion

        #region nonpublic methods
        private void UpdateCurrentValue()
        {
            if (!m_releasing)
            {
                if (m_timeInSamples < m_attackTime)
                    m_currentValue = m_startAmplitude + (1 - m_startAmplitude) * m_timeInSamples * m_attackRcpr;
                else if (m_timeInSamples < m_attackTime + m_decayTime)
                    m_currentValue = 1.0 - ((m_timeInSamples - m_attackTime) * m_decayRcpr) * (1.0 - m_sustainLevel);
                else
                    m_currentValue = m_sustainLevel;
            }
            else
                m_currentValue -= m_releaseStep;
            if (m_currentValue <= 0)
            {
                Playing = false;
                m_currentValue = 0;
            }
            ++m_timeInSamples;
        }
        #endregion

        #region public properties
        public double NextAmplitudeLinear
        {
            get
            {
                if (!Playing)
                    return 0;
                UpdateCurrentValue();
                return m_currentValue;
            }
        }

        public bool Playing
        {
            private set;
            get;
        }
        #endregion

        #region member variables
        private static int s_volumeLength = 256;
        private static double[] s_volumeMap = new double[s_volumeLength];
        private static bool s_initialized = false;
        private int m_attackTime;
        private double m_attackRcpr;
        private int m_decayTime;
        private double m_decayRcpr;
        private double m_sustainLevel;
        private double m_releaseTime;
        private double m_currentValue;
        private double m_releaseStep;
        private bool m_releasing;
        private int m_timeInSamples;
        private double m_startAmplitude;
        #endregion
    }
}
