namespace Orvina.Engine.Support
{
    internal class StarStateMachine
    {
        private readonly List<State> allStates;

        public StarStateMachine(TextBytes.SearchText searchText)
        {
            allStates = BuildMachine(searchText);
        }

        public int IndexOf(ReadOnlySpan<byte> input, TextBytes.SearchText searchText, out int endIdx, int startIdx = 0)
        {
            endIdx = 0;
            for (var i = startIdx; i < input.Length; i++)
            {
                var runMachine = searchText.upper[0] == TextBytes.questionMark
                    || (input[i] == TextBytes.questionMark && searchText.upper[0] == TextBytes.tilde && searchText.maxIdx > 0 && searchText.upper[1] == TextBytes.questionMark)
                    || (input[i] == TextBytes.asterisk && searchText.upper[0] == TextBytes.tilde && searchText.maxIdx > 0 && searchText.upper[1] == TextBytes.asterisk)
                    || (searchText.upper[0] == input[i] || searchText.lower[0] == input[i]);

                if (runMachine && Match(input.Slice(i, input.Length - i), out endIdx))
                {
                    endIdx = i + endIdx + 1;
                    return i;
                }
            }

            return -1;
        }

        public int IndexOf(ReadOnlySpan<byte> input, TextBytes.SearchText searchText, int startIdx = 0)
        {
            return IndexOf(input, searchText, out _, startIdx);
        }

        /// <summary>
        /// returns true if the string matches the pattern.
        /// "??xxT?123k**" matches "~?~?xx?~?*k~*~*"
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool Match(ReadOnlySpan<byte> input, out int endIdx)
        {
            endIdx = 0;
            var next = allStates[0];

            if (next.endState)
                return true;

            for (var i = 0; i < input.Length; i++)
            {
                next = ProcessState(next, input[i]);
                if (next.fail)
                {
                    next.fail = false;
                    return false;
                }
                else if (next.endState)
                {
                    endIdx = i;
                    return true;
                }
            }

            //endIdx = input.Length - 1;
            return false;
        }

        /// <summary>
        /// starting or ending with '*' not supported
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns></returns>
        private static List<State> BuildMachine(TextBytes.SearchText searchText)
        {
            var initialState = new State()
            {
                nextId = 1
            };

            //define the initial state
            switch (searchText.upper[0])
            {
                case TextBytes.tilde:
                    if (searchText.maxIdx > 0 && (searchText.upper[1] == TextBytes.questionMark || searchText.upper[1] == TextBytes.asterisk))
                    {
                        //tilde followed by ? or *
                        initialState.upperTrigger = initialState.lowerTrigger = searchText.upper[1];
                    }
                    else
                    {
                        //just a tilde
                        initialState.upperTrigger = initialState.lowerTrigger = TextBytes.tilde;
                    }
                    break;

                //case TextBytes.asterisk:
                //    if (searchText.length == 1)
                //    {
                //        initialState.endState = true;
                //    }
                //    else
                //    {
                //        initialState.stayOnNot = true;

                //        var idx = (searchText.upper[1] == TextBytes.tilde && searchText.length >= 3 && (searchText.upper[2] == TextBytes.questionMark || searchText.upper[2] == TextBytes.asterisk)) ? 2 : 1;
                //        initialState.lowerTrigger = searchText.lower[idx];
                //        initialState.upperTrigger = searchText.upper[idx];
                //    }
                //    break;

                case TextBytes.questionMark:
                    initialState.acceptAny = true;
                    break;

                default:
                    //just a tilde
                    initialState.lowerTrigger = searchText.lower[0];
                    initialState.upperTrigger = searchText.upper[0];
                    break;
            }

            //create the initial state
            var states = new List<State>(searchText.maxIdx)
            {
                initialState
            };

            for (var i = 0; i <= searchText.maxIdx; i++)
            {
                if (searchText.upper[i] != TextBytes.asterisk || (searchText.upper[i] == TextBytes.asterisk && i > 0 && searchText.upper[i - 1] == TextBytes.tilde))
                {
                    var newState = new State
                    {
                        nextId = states.Count + 1
                    };

                    var hasNext = i + 1 <= searchText.maxIdx;

                    if (hasNext)
                    {
                        var nextCharIdx = i + 1;
                        var nextChar = searchText.upper[nextCharIdx];

                        if (nextChar == TextBytes.tilde)
                        {
                            //tildes dont' matter except if followed by a * or ?
                            var hasCharAfterTilde = i + 2 <= searchText.maxIdx;
                            if (hasCharAfterTilde)
                            {
                                var nextNextChar = searchText.upper[i + 2];
                                if (nextNextChar == TextBytes.questionMark || nextNextChar == TextBytes.asterisk)
                                {
                                    newState.upperTrigger = newState.lowerTrigger = nextNextChar;
                                }
                                else
                                {
                                    newState.upperTrigger = searchText.upper[nextCharIdx];
                                    newState.lowerTrigger = searchText.lower[nextCharIdx];
                                }
                            }
                            else
                            {
                                //just a tilde
                                newState.upperTrigger = newState.lowerTrigger = TextBytes.tilde;
                            }
                        }
                        else if (nextChar == TextBytes.asterisk)//next is a star
                        {
                            if (searchText.upper[i] == TextBytes.tilde)//|| regex[i] == '*')
                            {
                                continue;
                            }
                            else
                            {
                                newState.stayOnNot = true;

                                //if star is at the end it might mean there is no trigger...!
                                bool hasCharAfterStar;
                                while ((hasCharAfterStar = i + 2 <= searchText.maxIdx) && searchText.upper[i + 2] == TextBytes.asterisk)
                                    i++;

                                if (hasCharAfterStar)
                                {
                                    var nextNextChar = searchText.upper[i + 2];
                                    if (nextNextChar == TextBytes.asterisk)
                                    {
                                        i++;
                                        //asterisk followed by asterisk?
                                    }

                                    //if the next character is a ~ and is followed by ? or *, then use that for trigger
                                    var idx = (nextNextChar == TextBytes.tilde && i + 3 <= searchText.maxIdx && (searchText.upper[i + 3] == TextBytes.questionMark || searchText.upper[i + 3] == TextBytes.asterisk)) ? i + 3 : i + 2;
                                    newState.lowerTrigger = searchText.lower[idx];
                                    newState.upperTrigger = searchText.upper[idx];
                                }
                            }
                        }
                        else if (nextChar == TextBytes.questionMark)
                        {
                            if (searchText.upper[i] == TextBytes.tilde)
                            {
                                continue;
                            }
                            else
                            {
                                newState.acceptAny = true;
                            }
                        }
                        else
                        {
                            newState.upperTrigger = searchText.upper[nextCharIdx];
                            newState.lowerTrigger = searchText.lower[nextCharIdx];
                        }
                    }
                    else
                    {
                        newState.endState = true;
                    }

                    states.Add(newState);
                }
            }

            return states;
        }

        private State ProcessState(State state, byte nextChar)
        {
            if (state.lowerTrigger == nextChar || state.upperTrigger == nextChar || (state.acceptAny && nextChar != TextBytes.newLine && nextChar != TextBytes.carriageReturn))
            {
                return allStates[state.nextId];
            }
            else if (state.stayOnNot)
            {
                return state;
            }
            else
            {
                state.fail = true;
                return state;
            }
        }

        private struct State
        {
            public bool acceptAny;
            public bool endState;
            public bool fail;
            public byte lowerTrigger;
            public int nextId;
            public bool stayOnNot;

            public byte upperTrigger;
        }
    }
}