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
// $Id: Compiler.cs 116 2009-05-23 14:39:36Z hikarin $
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if true
using SlMML.MonoTouch;
#else
using System.Windows.Controls;
using System.Windows.Threading;
#endif
using SlMML.Parsers;

namespace SlMML
{
    public delegate void CompileCompletedEventHandler(object sender, CompileCompletedEventArgs e);
    
    public class CompileCompletedEventArgs : EventArgs
    {
        public CompileCompletedEventArgs(MediaElement me)
        {
            m_element = me;
        }
        
        public MediaElement Result
        {
            get
            {
                return m_element;
            }
        }

        private MediaElement m_element;
    }

    public class Compiler
    {
        public event CompileCompletedEventHandler CompileCompleted;

        #region public class methods
        public static MediaElement Compile(string stringToParse)
        {
            MediaElement element = new MediaElement();
            IParsable parser = new FlMMLStyleParser();
            element.Volume = 1;
            element.SetSource(parser.Parse(stringToParse));
            return element;
        }
        #endregion

        #region public methods
        public void CompileAsync(string stringToParse)
        {
            m_stringToParse = stringToParse;
#if true
			CompileAsync_Tick (null, null);
#else
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromDays(1);
            timer.Tick += new EventHandler(CompileAsync_Tick);
            timer.Start();
            timer.Stop();
#endif
        }
#if DEBUG
        public static List<List<Dictionary<string, string>>> Dump(string stringToParse)
        {
            FlMMLStyleParser parser = new FlMMLStyleParser();
            parser.Parse(stringToParse);
            return parser.Dump();
        }
#endif
        #endregion

        #region nonpublic methods
        protected virtual void OnCompileCompleted(CompileCompletedEventArgs e)
        {
            if (CompileCompleted != null)
                CompileCompleted(this, e);
        }

        private void CompileAsync_Tick(object sender, EventArgs e)
        {
            MediaElement me = Compiler.Compile(m_stringToParse);
            OnCompileCompleted(new CompileCompletedEventArgs(me));
        }
        #endregion

        #region member variables
        private string m_stringToParse;
        #endregion
    }
}
