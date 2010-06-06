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
// $Id: Oscillator.cs 117 2009-05-23 15:29:19Z hikarin $
//

using System;
using SlMML.Modulators;

namespace SlMML
{
    public enum OscillatorForm
    {
        Sine,
        Saw,
        Triangle,
        Pulse,
        Noise,
        FCPulse,
        FCTriangle,
        FCNoise,
        FCShortNoise,
        FCDPCM,
        GBWave,
        GBLongNoise,
        GBShortNoise,
        Max
    }
    
    public class Oscillator
    {
        #region constructors and destructor
        public Oscillator()
        {
            m_modulators = new IModulator[(int)OscillatorForm.Max];
            m_modulators[(int)OscillatorForm.Sine] = new Sine();
            m_modulators[(int)OscillatorForm.Saw] = new Saw();
            m_modulators[(int)OscillatorForm.Triangle] = new Triangle();
            m_modulators[(int)OscillatorForm.Pulse] = new Pulse();
            m_modulators[(int)OscillatorForm.Noise] = new Noise();
            m_modulators[(int)OscillatorForm.FCPulse] = new Pulse();
            m_modulators[(int)OscillatorForm.FCTriangle] = new FCTriangle();
            m_modulators[(int)OscillatorForm.FCNoise] = new FCNoise();
            m_modulators[(int)OscillatorForm.FCShortNoise] = new FCNoise();
            m_modulators[(int)OscillatorForm.FCDPCM] = new FCDPCM();
            m_modulators[(int)OscillatorForm.GBWave] = new GBWave();
            m_modulators[(int)OscillatorForm.GBLongNoise] = new GBLongNoise();
            m_modulators[(int)OscillatorForm.GBShortNoise] = new GBShortNoise();
            Form = OscillatorForm.Pulse;
        }
        #endregion

        #region public methods
        public IModulator ModulatorFromForm(OscillatorForm form)
        {
            int index = Math.Min(Math.Max((int)form, 0), (int)(OscillatorForm.Max - 1));
            return m_modulators[index];
        }
        
        public void MakeAsLFO()
        {
            if (m_modulators[(int)OscillatorForm.Noise] != null)
            {
                Noise noise = (Noise)m_modulators[(int)OscillatorForm.Noise];
                noise.ShouldResetPhase = false;
            }
        }
        #endregion

        #region public properties
        public OscillatorForm Form
        {
            set
            {
                int index = Math.Min(Math.Max((int)value, 0), (int)(OscillatorForm.Max - 1));
                m_form = (OscillatorForm)index;
                switch (m_form)
                {
                    case OscillatorForm.Noise:
                        Noise noise = (Noise)m_modulators[(int)OscillatorForm.Noise];
                        noise.RestoreFrequency();
                        break;
                    case OscillatorForm.FCNoise:
                        FCNoise fcNoise = (FCNoise)m_modulators[(int)OscillatorForm.FCNoise];
                        fcNoise.SetLongMode();
                        break;
                    case OscillatorForm.FCShortNoise:
                        FCNoise fcShortNoise = (FCNoise)m_modulators[(int)OscillatorForm.FCShortNoise];
                        fcShortNoise.SetShortMode();
                        break;
                    default:
                        break;
                }
            }
            get
            {
                return m_form;
            }
        }
        
        public IModulator CurrentModulator
        {
            get
            {
                return m_modulators[(int)Form];
            }
        }
        #endregion

        #region member variables
        IModulator[] m_modulators;
        OscillatorForm m_form;
        #endregion
    }
}
