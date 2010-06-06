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
// $Id: FlMMLStyleParser.cs 116 2009-05-23 14:39:36Z hikarin $
//

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SlMML.Parsers
{
    class FlMMLStyleParser : Parser, IParsable
    {
        struct MacroArgument
        {
            public string Name;
            public int Index;
        }

        struct MacroValue
        {
            public StringBuilder Code;
            public List<MacroArgument> Arguments;
        }

        #region nonpublic class methods
        private static void GetArgumentFromKey(string key, out List<MacroArgument> arguments)
        {
            arguments = new List<MacroArgument>();
            int start = key.IndexOf('{');
            int end = key.IndexOf('}') - start - 1;
            if (start >= 0 && end > 0)
            {
                string[] args = key.Substring(start + 1, end).Split(',');
                int i = 0;
                foreach (string arg in args)
                {
                    string name = arg;
                    if (ValidateMacroName(ref name))
                    {
                        arguments.Add(new MacroArgument { Name = name, Index = i });
                        i++;
                    }
                }
                arguments.Sort((x, y) =>
                    {
                        int xl = x.Name.Length, yl = y.Name.Length;
                        if (xl > yl)
                            return -1;
                        else if (xl == yl)
                            return 0;
                        else
                            return 1;
                    });
            }
        }
        
        private static void SortMacros(ref Dictionary<string, MacroValue> macros)
        {
            List<KeyValuePair<string, MacroValue>> pairs = new List<KeyValuePair<string, MacroValue>>(macros);
            pairs.Sort((x, y) =>
            {
                int xl = x.Key.Length, yl = y.Key.Length;
                if (xl > yl)
                    return -1;
                else if (xl == yl)
                    return 0;
                else
                    return 1;
            });
            macros.Clear();
            foreach (KeyValuePair<string, MacroValue> pair in pairs)
                macros[pair.Key] = pair.Value;
        }
        
        private static bool ValidateMacroName(ref string name)
        {
            char f = name[0];
            StringBuilder s = new StringBuilder();
            if (!char.IsLetter(f) && f != '_' && f != '#' && f != '+' && f != '(' && f != ')')
                return false;
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '#' || c == '+' || c == '(' || c == ')')
                    s.Append(c);
                else
                    break;
            }
            name = s.ToString();
            return true;
        }
        #endregion

        #region public methods
        public Sequencer Parse(string stringToParse)
        {
            ParseBefore(stringToParse);
            ParseComment();
            ParseMacro();
            RemoveSpaces(ref m_text);
            ToLower(ref m_text);
            ParseRepeat();
            m_index = 0;
            int length = m_text.Length;
            while (m_index < length)
                FirstLetterToken();
            return ParseAfter();
        }
        #endregion

        #region nonpublic methods
        private void ParseComment()
        {
            int start = -1, textLength = m_text.Length;
            m_index = 0;
            while (m_index < textLength)
            {
                char c = CharacterTokenAndNext();
                switch (c)
                {
                    case '/':
                        if (CharacterToken() == '*')
                        {
                            if (start < 0)
                                start = m_index - 1;
                            NextToken();
                        }
                        break;
                    case '*':
                        if (CharacterToken() == '/')
                        {
                            if (start >= 0)
                            {
                                m_text.Remove(start, m_index + 1 - start);
                                m_index = start;
                                textLength = m_text.Length;
                                start = -1;
                            }
                            else
                                m_warnings.Add(CompilerWarning.UnOpenedComment);
                        }
                        break;
                    default:
                        break;
                }
            }
            if (start >= 0)
                m_warnings.Add(CompilerWarning.UnClosedComment);
        }
        
        private bool ReplaceMacro(Dictionary<string, MacroValue> macros)
        {
            foreach (KeyValuePair<string, MacroValue> macro in macros)
            {
                string key = macro.Key;
                int keyLength = key.Length;
                if (m_text.ToString().Substring(m_index, keyLength).Equals(key))
                {
                    int start = m_index, last = start + keyLength;
                    string code = macro.Value.Code.ToString();
                    m_index += keyLength;
                    char c = CharacterToken();
                    while (char.IsWhiteSpace(c))
                        c = CharacterTokenAndNext();
                    string[] args = new string[0];
                    if (c == '{')
                    {
                        c = CharacterTokenAndNext();
                        while (c != '}' && c != char.MinValue)
                        {
                            if (c == '$')
                                ReplaceMacro(macros);
                            c = CharacterTokenAndNext();
                        }
                        last = m_index;
                        int from = start + keyLength + 1, length = last - 1 - from;
                        args = m_text.ToString().Substring(from, length).Split(',');
                    }
                    List<MacroArgument> arguments = macro.Value.Arguments;
                    int codeLength = code.Length, argsLength = args.Length;
                    int macroArgumentCount = arguments.Count;
                    for (int i = 0; i < codeLength; i++)
                    {
                        for (int j = 0; j < argsLength; j++)
                        {
                            if (j >= macroArgumentCount)
                                break;
                            string argumentKey = arguments[j].Name, subs = "%" + argumentKey;
                            int subsLength = subs.Length, index = arguments[j].Index;
                            if (codeLength >= i + subsLength && code.Substring(i, subsLength).Equals(subs))
                            {
                                code = code.Substring(0, i) + code.Substring(i).Replace(subs, args[index]);
                                codeLength = code.Length;
                                i += args[arguments[j].Index].Length - 1;
                                break;
                            }
                        }
                    }
                    string chunk = m_text.ToString();
                    m_text = new StringBuilder(chunk.Substring(0, start - 1) + code + chunk.Substring(last));
                    m_index = start - 1;
                    return true;
                }
            }
            return false;
        }
        
        private void ParseMacro()
        {
            StringBuilder s = new StringBuilder();
            using (StringReader reader = new StringReader(m_text.ToString()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("#OCTAVE"))
                    {
                        if (line.Substring(7).TrimStart().StartsWith("REVERSE"))
                            m_relativeDir = false;
                    }
                    else if (line.StartsWith("#WAV9"))
                    {
                        string[] wave = line.Substring(5).TrimStart().Split(",".ToCharArray());
                        if (wave.Length == 4)
                        {
                            int waveIndex = 0, intVol = 0, loopFlag = 0;
                            int.TryParse(wave[0], out waveIndex);
                            int.TryParse(wave[1], out intVol);
                            int.TryParse(wave[2], out loopFlag);
                            Modulators.FCDPCM.SetWave(waveIndex, intVol, loopFlag, wave[3]);
                        }
                    }
                    else if (line.StartsWith("#WAV10"))
                    {
                        int waveNo = 0;
                        string[] wave = line.Substring(6).TrimStart().Split(",".ToCharArray());
                        if (wave.Length == 2 && int.TryParse(wave[0], out waveNo))
                        {
                            string waveString = wave[1] + "00000000000000000000000000000000";
                            Modulators.GBWave.SetWaveString(waveString, waveNo);
                        }
                    }
                    else
                    {
                        s.Append(line);
                        s.Append("\n");
                    }
                }
            }
            m_text = s;
            m_index = 0;
            bool top = true;
            int last = 0, textLength = m_text.Length;
            Dictionary<string, MacroValue> macros = new Dictionary<string, MacroValue>();
            while (m_index < textLength)
            {
                char c = CharacterTokenAndNext();
                switch (c)
                {
                    case '$':
                        if (top)
                        {
                            string chunk = m_text.ToString();
                            last = chunk.IndexOf(';', m_index, m_text.Length - m_index);
                            if (last >= 0 && last > m_index)
                            {
                                string macro = chunk.Substring(m_index, last - m_index);
                                string[] kv = macro.Split('=');
                                if (kv.Length == 2 && kv[0].Length > 0)
                                {
                                    int macroStartAt = m_index;
                                    int keyEndAt = m_text.ToString().IndexOf('=');
                                    string key = kv[0], name = key;
                                    if (ValidateMacroName(ref name))
                                    {
                                        int keyLength = key.Length;
                                        List<MacroArgument> arguments;
                                        GetArgumentFromKey(key, out arguments);
                                        m_index = keyEndAt + 1;
                                        c = CharacterTokenAndNext();
                                        while (m_index < last)
                                        {
                                            if (c == '$')
                                            {
                                                if (!ReplaceMacro(macros))
                                                {
                                                    if (m_text.ToString().Substring(m_index, keyLength).Equals(key))
                                                    {
                                                        m_index--;
                                                        m_text.Remove(m_index, key.Length);
                                                        m_warnings.Add(CompilerWarning.RecursiveMacro);
                                                    }
                                                }
                                                last = m_text.ToString().IndexOf(';', m_index);
                                            }
                                            c = CharacterTokenAndNext();
                                        }
                                        int codeLength = last - keyEndAt - 1;
                                        MacroValue value = new MacroValue()
                                        {
                                            Code = new StringBuilder(m_text.ToString().Substring(keyEndAt + 1, codeLength)),
                                            Arguments = arguments
                                        };
                                        macros[name] = value;
                                        SortMacros(ref macros);
                                        int from = macroStartAt - 1, to = last - from;
                                        m_text.Remove(from, to);
                                        m_index = macroStartAt - 1;
                                        textLength = m_text.Length;
                                    }
                                }
                                else
                                {
                                    ReplaceMacro(macros);
                                    textLength = m_text.Length;
                                    top = false;
                                }
                            }
                            else
                            {
                                ReplaceMacro(macros);
                                textLength = m_text.Length;
                                top = false;
                            }
                        }
                        else
                        {
                            ReplaceMacro(macros);
                            textLength = m_text.Length;
                            top = false;
                        }
                        break;
                    case ';':
                        top = true;
                        break;
                    default:
                        if (!char.IsWhiteSpace(c))
                            top = false;
                        break;
                }
            }
        }
        
        private void ParseRepeat()
        {
            List<int> repeats = new List<int>();
            List<int> origins = new List<int>();
            List<int> starts = new List<int>();
            List<int> lasts = new List<int>();
            int nest = -1, textLength = m_text.Length;
            m_index = 0;
            while (m_index < textLength)
            {
                char c = CharacterTokenAndNext();
                switch (c)
                {
                    case '/':
                        if (CharacterToken() == ':')
                        {
                            NextToken();
                            ++nest;
                            origins.Add(m_index - 2);
                            repeats.Add((int)UIntToken(2));
                            starts.Add(m_index);
                            lasts.Add(-1);
                        }
                        else if (nest >= 0)
                        {
                            lasts[nest] = m_index - 1;
                            m_text.Remove(m_index - 1, 1);
                            textLength = m_text.Length;
                            --m_index;
                        }
                        break; 
                    case ':':
                        if (CharacterToken() == '/' && nest >= 0)
                        {
                            NextToken();
                            int start = starts[nest];
                            int last = lasts[nest];
                            int repeat = repeats[nest];
                            string chunk = m_text.ToString();
                            string contents = chunk.Substring(start, m_index - 2 - start);
                            StringBuilder newString = new StringBuilder(chunk.Substring(0, origins[nest]));
                            for (int i = 0; i < repeat; i++)
                                if (i < repeat - 1 || last < 0)
                                    newString.Append(contents);
                                else
                                    newString.Append(m_text.ToString().Substring(start, last - start));
                            int newStringLength = newString.Length;
                            newString.Append(m_text.ToString().Substring(m_index));
                            m_text = newString;
                            m_index = newStringLength;
                            textLength = newString.Length;
                            origins.RemoveAt(nest);
                            repeats.RemoveAt(nest);
                            starts.RemoveAt(nest);
                            lasts.RemoveAt(nest);
                            --nest;
                        }
                        break;
                    default:
                        break;
                }
            }
            if (nest >= 0)
                m_warnings.Add(CompilerWarning.UnClosedRepeat);
        }
        #endregion
    }
}
