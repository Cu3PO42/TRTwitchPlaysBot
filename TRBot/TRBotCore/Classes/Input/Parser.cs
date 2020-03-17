﻿/* Original Python parser for TPE written in Python by: Jdog, aka TwitchPlays_Everything
 * Converted to C# by: Kimimaru, aka Kimimaru4000
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace TRBot
{
    /// <summary>
    /// The parser for input.
    /// </summary>
    /// <remarks>
    /// Kimimaru: Constant Regex expressions are compiled to increase performance of subsequent Match calls.
    /// This is favorable since we have only a few Regex expressions that are run many times.
    /// </remarks>
    public static class Parser
    {
        private static Comparison<(string, (int, int), List<string>)> SubCompare = SubComparison;

        static Parser()
        {
            //Set Regex cache size
            Regex.CacheSize = 32;
        }

        public static string Expandify(string message)
        {
            const string regex = @"\[([^\[\]]*\])\*(\d{1,2})";
            Match m = Regex.Match(message, regex, RegexOptions.Compiled);
            while (m.Success == true)
            {
                string str = string.Empty;
                string value = m.Groups[1].Value.Replace("]", string.Empty).Replace("[", string.Empty);

                int number = 0;
                if (int.TryParse(m.Groups[2].Value, out number) == false)
                {
                    return message;
                }

                for (int i = 0; i < number; i++)
                {
                    str += value;
                }

                string start = message.Substring(0, m.Index);
                string end = message.Substring(m.Groups[2].Index + m.Groups[2].Length);

                message = start + str + end;
                m = Regex.Match(message, regex, RegexOptions.Compiled);
            }
            return message;
        }

        public static string PopulateVariables(string macro_contents, List<string> variables)
        {
            int count = variables.Count;
            for (int i = 0; i < count; i++)
            {
                string v = variables[i];
                macro_contents = Regex.Replace(macro_contents, "<" + i + ">", v);
            }
            return macro_contents;
        }

        public static string PopulateMacros(string message)
        {
            message = message.Replace(" ", string.Empty);
            message = Parser.Expandify(message);

            const int MAX_RECURSION = 10;
            int count = 0;
            bool found_macro = true;
            Match macro_args = null;
            List<string> macro_argsarr = null;
            while (count < MAX_RECURSION && found_macro == true)
            {
                found_macro = false;
                MatchCollection possible_macros = Regex.Matches(message, @"#[a-zA-Z0-9\(\,\.]*", RegexOptions.Compiled);
                List<(string, (int, int), List<string>)> subs = null;
                foreach (Match p in possible_macros)
                {
                    string macro_name = Regex.Replace(message.Substring(p.Index, p.Length), @"\(.*\)", string.Empty, RegexOptions.Compiled);
                    string macro_name_generic = string.Empty;
                    int arg_index = macro_name.IndexOf("(");
                    if (arg_index != -1)
                    {
                        string sub = message.Substring(p.Index, p.Length + 1);
                        macro_args = Regex.Match(sub, @"\(.*\)", RegexOptions.Compiled);
                        if (macro_args.Success == true)
                        {
                            int start = p.Index + macro_args.Index + 1;
                            string substr = message.Substring(start, macro_args.Length - 2);
                            macro_argsarr = new List<string>(substr.Split(","));
                            macro_name += ")";
                            macro_name_generic = Regex.Replace(macro_name, @"\(.*\)", string.Empty, RegexOptions.Compiled) + "(";

                            int macroArgsCount = macro_argsarr.Count;
                            for (int i = 0; i < macroArgsCount; i++)
                            {
                                macro_name_generic += "*,";
                            }
                            macro_name_generic = macro_name_generic.Substring(0, macro_name_generic.Length - 1) + ")";
                        }
                    }
                    else
                    {
                        macro_argsarr = new List<string>();
                        macro_name_generic = macro_name;
                    }

                    string longest = string.Empty;
                    int end = 0;

                    //Look through the parser macro list for performance
                    if (BotProgram.BotData.ParserMacroLookup.TryGetValue(macro_name_generic[1], out List<string> macroList) == true)
                    {
                        for (int i = 0; i < macroList.Count; i++)
                        {
                            string macro = macroList[i];
                    
                            if (macro_name_generic.Contains(macro) == true)
                            {
                                if (macro.Length > longest.Length) longest = macro;
                            }
                            end = p.Index + longest.Length;
                        }
                    }

                    if (string.IsNullOrEmpty(longest) == false)
                    {
                        if (subs == null)
                            subs = new List<(string, (int, int), List<string>)>(4);

                        if (macro_argsarr.Count > 0)
                        {
                            subs.Add((longest, (p.Index, p.Index + p.Length + 1), macro_argsarr));
                        }
                        else
                        {
                            subs.Add((longest, (p.Index, end), macro_argsarr));
                        }
                    }
                }

                string str = string.Empty;
                if (subs?.Count > 0)
                {
                    //Sort by start of the macro index
                    subs.Sort(SubCompare);

                    found_macro = true;
                    str = message.Substring(0, subs[0].Item2.Item1);
                    (string, (int, int), List<string>) def = default;
                    (string, (int, int), List<string>) prev = default;
                    foreach (var current in subs)
                    {
                        if (prev != def) str += message.Substring(prev.Item2.Item2, current.Item2.Item1 - prev.Item2.Item2);
                        str += Parser.PopulateVariables(BotProgram.BotData.Macros[current.Item1], current.Item3);
                        prev = current;
                    }
                    str += message.Substring(prev.Item2.Item2);
                    message = str;
                }
                count += 1;
            }

            return message;
        }

        public static string PopulateSynonyms(string message)
        {
            foreach (string synonym in InputGlobals.INPUT_SYNONYMS.Keys)
            {
                message = message.Replace(synonym, InputGlobals.INPUT_SYNONYMS[synonym]);
            }

            return message;
        }

        //Returns Input object
        private static Input GetInput(string message)
        {
            //Create a default input instance
            Input current_input = Input.Default;

            const string regex = @"^[_-]";
            Match m = Regex.Match(message, regex, RegexOptions.Compiled);

            //If there's a match, trim the message
            if (m.Success == true)
            {
                string c = message.Substring(m.Index, m.Length);
                message = message.Substring(m.Length);

                if (c == "_")
                {
                    current_input.hold = true;
                    current_input.length += 1;
                }
                else if (c == "-")
                {
                    current_input.release = true;
                    current_input.length += 1;
                }
            }

            //Try to match one input, prioritizing the longest match
            int max = 0;
            string valid_input = string.Empty;

            foreach (string button in InputGlobals.ValidInputs)
            {
                if (button == ".")
                    m = Regex.Match(message, @"^\.", RegexOptions.Compiled);
                else
                    m = Regex.Match(message, $"^{button}");

                if (m.Success == true)
                {
                    int length = (m.Index + m.Length) - m.Index;

                    if (length > max)
                    {
                        max = length;
                        current_input.name = message.Substring(m.Index, m.Length);
                    }
                }
            }

            //If not a valid input, break parsing
            if (string.IsNullOrEmpty(current_input.name) == true)
            {
                current_input.error = "ERR_INVALID_INPUT";

                return current_input;
            }
            else
                current_input.length += max;

            //Trim the input from the message
            message = message.Substring(max);

            //Try to match a percent
            const string percentRegex = @"^\d+%";
            m = Regex.Match(message, percentRegex, RegexOptions.Compiled);

            if (m.Success == true)
            {
                current_input.percent = int.Parse(message.Substring(m.Index, m.Length - 1));
                message = message.Substring(m.Length);
                current_input.length += current_input.percent.ToString().Length + 1;

                if (current_input.percent > 100)
                {
                    current_input.error = "ERR_INVALID_PERCENTAGE";
                    return current_input;
                }
            }

            //Try to match a duration
            const string durationRegex = @"^\d+";
            m = Regex.Match(message, durationRegex, RegexOptions.Compiled);

            if (m.Success == true)
            {
                current_input.duration = int.Parse(message.Substring(m.Index, m.Length));
                message = message.Substring(m.Length);
                current_input.length += current_input.duration.ToString().Length;

                //Determine the type of duration
                const string durTypeRegex = @"(s|ms)";
                m = Regex.Match(message, durTypeRegex, RegexOptions.Compiled);

                if (m.Success == true)
                {
                    current_input.duration_type = message.Substring(m.Index, m.Length);
                    message = message.Substring(m.Length);

                    if (current_input.duration_type == "s")
                    {
                        current_input.duration *= 1000;
                        current_input.length += 1;
                    }
                    else
                        current_input.length += 2;
                }
                else
                {
                    current_input.error = "ERR_DURATION_TYPE_UNSPECIFIED";
                    return current_input;
                }
            }
            return current_input;
        }

        //NOTE: TEST METHOD FOR NEW PARSING - DO NOT USE YET
        public static void ParseInputs(string message)
        {
            bool validInput = true;

            message = message.Replace(" ", string.Empty).ToLower();
            message = PopulateSynonyms(message);

            //Full Regex:
            // ([_-])?(left|right|a|b|l|r){1}(\d*)(ms|s)?
            //Replace "left", "right", etc. with all the inputs for the console

            //Group 1 = zero or one of '_' or '-' for hold and subtract, respectively
            //Group 2 = the input - exactly one
            //Group 3 = the number for duration
            //Group 4 = ms or s - the duration type

            string regex = "([_-])?(";
            for (int i = 0; i < InputGlobals.ValidInputs.Length; i++)
            {
                regex += Regex.Escape(InputGlobals.ValidInputs[i]);

                if (i == InputGlobals.ValidInputs.Length - 1)
                    regex += ")";
                else regex += "|";
            }

            regex += @"{1}(\d*)(ms|s)?";

            //Console.WriteLine(regex);

            //New method: Get ALL the matches at once and parse them as we go, instead of matching each time and parsing

            MatchCollection matches = Regex.Matches(message, regex, RegexOptions.IgnoreCase);
            //Console.WriteLine(matches.Count);

            foreach (Match m in matches)
            {
                Console.WriteLine($"Name: {m.Index}");
            }

            //Console.WriteLine($"Valid input: {validInput}");
        }

        //Returns list containing: [Valid, input_sequence]
        //Or: [Invalid, input that it failed on]
        public static (bool, List<List<Input>>, bool, int) Parse(string message)
        {
            bool contains_start_input = false;
            message = message.Replace(" ", string.Empty).ToLower();
            List<Input> input_subsequence = null;
            List<List<Input>> input_sequence = new List<List<Input>>(8);
            int duration_counter = 0;

            message = PopulateSynonyms(message);

            while (message.Length > 0)
            {
                input_subsequence = new List<Input>(8);
                int subduration_max = 0;
                Input current_input = GetInput(message);
                
                if (string.IsNullOrEmpty(current_input.error) == false)
                    return (false, new List<List<Input>>(1) { new List<Input>(1) { current_input } }, false, subduration_max);

                message = message.Substring(current_input.length);
                input_subsequence.Add(current_input);

                if (current_input.duration > subduration_max)
                    subduration_max = current_input.duration;

                if (message.Length > 0)
                {
                    while (message[0] == '+')
                    {
                        if (message.Length > 0)
                            message = message.Substring(1);
                        else
                            break;

                        current_input = GetInput(message);

                        /*
                         * if (current_input.name == "plus")
                         *     contains_start_input = true;
                         */

                        if (string.IsNullOrEmpty(current_input.error) == false)
                            return (false, new List<List<Input>>(1) { new List<Input>(1) { current_input }  }, false, subduration_max);

                        message = message.Substring(current_input.length);
                        input_subsequence.Add(current_input);

                        if (current_input.duration > subduration_max)
                            subduration_max = current_input.duration;

                        if (message.Length == 0)
                            break;
                    }
                }

                duration_counter += subduration_max;

                if (duration_counter > BotProgram.BotData.MaxInputDuration)
                {
                    current_input.error = "ERR_DURATION_MAX";
                    return (false, new List<List<Input>>(1) { new List<Input>(1) { current_input }  }, false, subduration_max);
                }

                input_sequence.Add(input_subsequence);
            }

            return (true, input_sequence, contains_start_input, duration_counter);
        }

        private static int SubComparison((string, (int, int), List<string>) val1, (string, (int, int), List<string>) val2)
        {
            return val1.Item2.Item1.CompareTo(val2.Item2.Item1);
        }

        /// <summary>
        /// Contains input data.
        /// </summary>
        public struct Input
        {
            public string name;
            public bool hold;
            public bool release;
            public int percent;
            public int duration;
            public string duration_type;
            public int length;
            public string error;

            /// <summary>
            /// Returns a default Input.
            /// </summary>
            public static Input Default => new Input(string.Empty, false, false, 100, BotProgram.BotData.DefaultInputDuration, "ms", 0, string.Empty);

            public Input(string nme, in bool hld, in bool relse, in int percnt, in int dur, string durType, in int len, in string err)
            {
                this.name = nme;
                this.hold = hld;
                this.release = relse;
                this.percent = percnt;
                this.duration = dur;
                this.duration_type = durType;
                this.length = 0;
                this.error = string.Empty;
            }

            public override string ToString()
            {
                return $"\"{name}\" {duration}{duration_type} | H:{hold} | R:{release} | P:{percent} | Err:{error}";
            }
        }
    }
}
