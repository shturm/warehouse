//
// RPNCalculator.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   09/23/2008
//
// 2006-2015 (C) Microinvest, http://www.microinvest.net
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Warehouse.Data.Calculator
{
    public class RPNCalculator
    {
        private enum TokenParserState
        {
            Number,
            Function,
            Operator,
            WhiteSpace,
            None
        }

        public static List<Token> ParseTokens (string input)
        {
            if (input == null)
                throw new ArgumentNullException ("input");

            NumberFormatInfo numberFormat = NumberFormatInfo.CurrentInfo;
            TokenParserState oldState = TokenParserState.None;
            TokenParserState newState = TokenParserState.None;
            List<char> number = new List<char> ();
            List<char> function = new List<char> ();
            Operator newOper = null;
            List<Token> tokens = new List<Token> ();
            int i = 0;

            foreach (char c in input) {
                if (Char.IsDigit (c) ||
                    c.ToString () == numberFormat.NumberDecimalSeparator ||
                    c.ToString () == numberFormat.NumberGroupSeparator) {
                    number.Add (c);
                    newState = TokenParserState.Number;
                } else if (Char.IsWhiteSpace (c)) {
                    if (number.Count > 0 &&
                        (Char.IsWhiteSpace (numberFormat.NumberDecimalSeparator, 0) ||
                         Char.IsWhiteSpace (numberFormat.NumberGroupSeparator, 0))) {
                        number.Add (c);
                        newState = TokenParserState.Number;
                    } else {
                        newState = TokenParserState.WhiteSpace;
                    }
                } else if (c == ',' || c == '.') {
                    if (number.Count == 0)
                        throw new ExpressionErrorException (i);

                    number.Add (c);
                } else if (Char.IsLetter (c)) {
                    function.Add (c);
                    newState = TokenParserState.Function;
                } else if (c == '+') {
                    newOper = new Operator (OperatorType.Plus);
                    newState = TokenParserState.Operator;
                } else if (c == '-') {
                    newOper = new Operator (OperatorType.Minus);
                    newState = TokenParserState.Operator;
                } else if (c == '*') {
                    newOper = new Operator (OperatorType.Multiply);
                    newState = TokenParserState.Operator;
                } else if (c == '/') {
                    newOper = new Operator (OperatorType.Divide);
                    newState = TokenParserState.Operator;
                } else if (c == '%') {
                    newOper = new Operator (OperatorType.Modulus);
                    newState = TokenParserState.Operator;
                } else if (c == '^') {
                    newOper = new Operator (OperatorType.Power);
                    newState = TokenParserState.Operator;
                } else if (c == '(') {
                    newOper = new Operator (OperatorType.OpenBrace);
                    newState = TokenParserState.Operator;
                } else if (c == ')') {
                    newOper = new Operator (OperatorType.CloseBrace);
                    newState = TokenParserState.Operator;
                } else if (c == '\\') {
                    newOper = new Operator (OperatorType.IntegerDivision);
                    newState = TokenParserState.Operator;
                } else {
                    throw new ExpressionErrorException (string.Format ("Unable to parse character '{0}'", c));
                }

                switch (oldState) {
                    case TokenParserState.Number:
                        if (newState != TokenParserState.Number) {
                            EvaluateNumber (number, tokens);
                        }
                        break;
                    case TokenParserState.Function:
                        if (newState != oldState) {
                            tokens.Add (new Function (new string (function.ToArray ())));
                            function.Clear ();
                        }
                        break;
                }

                if (newOper != null) {
                    tokens.Add (newOper);
                    newOper = null;
                }

                oldState = newState;
                i++;
            }

            if (oldState == TokenParserState.Number)
                EvaluateNumber (number, tokens);

            if (tokens.Count == 0)
                throw new ExpressionErrorException ("No tokens found.");

            return tokens;
        }

        private static void EvaluateNumber (List<char> number, IList<Token> tokens)
        {
            bool addMinus = false;

            if (tokens.Count > 0) {
                Operator oper = tokens [tokens.Count - 1] as Operator;
                if (oper != null && (oper.Type == OperatorType.Minus || oper.Type == OperatorType.Plus)) {
                    bool addSign = false;
                    if (tokens.Count > 1) {
                        Operator oper1 = tokens [tokens.Count - 2] as Operator;
                        if (oper1 != null)
                            addSign = true;
                    } else {
                        addSign = true;
                    }

                    if (addSign) {
                        tokens.RemoveAt (tokens.Count - 1);
                        if (oper.Type == OperatorType.Minus)
                            addMinus = true;
                    }
                }
            }

            if (addMinus)
                number.Insert (0, '-');

            NumberFormatInfo n = new NumberFormatInfo ();
            n.NumberDecimalSeparator = ".";
            n.NumberGroupSeparator = string.Empty;
            bool decimaSeparatorFound = false;

            for (int i = number.Count - 1; i >= 0; i--) {
                if (char.IsDigit (number [i]) || number [i] == '-')
                    continue;

                if ((number [i] == ',' || number [i] == '.') && !decimaSeparatorFound) {
                    number [i] = '.';
                    decimaSeparatorFound = true;
                    continue;
                }

                number.RemoveAt (i);
            }

            string num = new string (number.ToArray ());
            double res;
            if (!double.TryParse (num, NumberStyles.Any, n, out res))
                throw new ExpressionErrorException ("Cannot parse number: " + num);

            tokens.Add (new Operand (res));
            number.Clear ();
        }

        public static List<Token> ConvertInfixToRPN (IList<Token> input)
        {
            List<Token> ret = new List<Token> ();
            Stack<Token> stack = new Stack<Token> ();

            foreach (Token token in input) {
                Operator opr = token as Operator;
                Operand operand = token as Operand;

                if (operand != null) {
                    ret.Add (operand);
                    continue;
                }

                if (opr != null) {
                    bool braceMatched = false;
                    while (stack.Count > 0) {
                        Operator stackOpr = (Operator) stack.Peek ();
                        if (stackOpr.Type == OperatorType.OpenBrace) {
                            if (opr.Type == OperatorType.CloseBrace) {
                                stack.Pop ();
                                braceMatched = true;
                            }
                            break;
                        } else if (stackOpr.Precedance >= opr.Precedance) {
                            stack.Pop ();
                            ret.Add (stackOpr);
                        } else
                            break;
                    }

                    if (opr.Type != OperatorType.CloseBrace)
                        stack.Push (opr);

                    if (opr.Type == OperatorType.CloseBrace && !braceMatched)
                        throw new ExpressionErrorException ("Braces are not matching");
                }
            }

            while (stack.Count > 0) {
                Operator stackOpr = (Operator) stack.Pop ();
                if (stackOpr.Type == OperatorType.OpenBrace)
                    throw new ExpressionErrorException ("Braces are not matching");

                ret.Add (stackOpr);
            }

            return ret;
        }

        public static double EvaluateRPN (IList<Token> input)
        {
            if (input == null)
                throw new ArgumentNullException ("input");

            if (input.Count == 0)
                return 0;

            Stack<Operand> stack = new Stack<Operand> ();

            foreach (Token token in input) {
                Operator opr = token as Operator;
                Operand opn = token as Operand;

                if (opn != null) {
                    stack.Push (opn);
                    continue;
                }

                if (opr != null) {
                    List<Operand> operands = new List<Operand> ();
                    while (operands.Count < opr.ParametersCount) {
                        if (stack.Count == 0) {
                            if (operands.Count < opr.MinimalParametersCount)
                                throw new ExpressionErrorException ("Invalid number of operands");

                            break;
                        } else
                            operands.Insert (0, stack.Pop ());
                    }
                    stack.Push (new Operand (opr.Calculate (operands.ToArray ())));
                }
            }

            if (stack.Count > 1)
                throw new ExpressionErrorException ("Invalid number of operands");

            return stack.Pop ().Value;
        }

        public static double EvaluateExpression (string input)
        {
            List<Token> infixExpression = ParseTokens (input);
            List<Token> rpnExpression = ConvertInfixToRPN (infixExpression);

            return EvaluateRPN (rpnExpression);
        }
    }
}
