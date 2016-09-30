//
// Operator.cs
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

namespace Warehouse.Data.Calculator
{
    public enum OperatorType
    {
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulus,
        Power,
        OpenBrace,
        CloseBrace,
        IntegerDivision,
        Unknown
    }

    public class Operator : Token
    {
        private readonly OperatorType type;

        public OperatorType Type
        {
            get { return type; }
        }

        public Operator (OperatorType type)
        {
            this.type = type;
        }

        public int Precedance
        {
            get
            {
                switch (type) {
                    case OperatorType.Plus:
                        return 1;
                    case OperatorType.Minus:
                        return 1;
                    case OperatorType.Multiply:
                        return 2;
                    case OperatorType.Divide:
                    case OperatorType.IntegerDivision:
                        return 2;
                    case OperatorType.Modulus:
                        return 2;
                    case OperatorType.Power:
                        return 3;
                    case OperatorType.OpenBrace:
                        return 4;
                    case OperatorType.CloseBrace:
                        return 0;
                    default:
                        return 0;
                }
            }
        }

        public int ParametersCount
        {
            get
            {
                switch (type) {
                    case OperatorType.Plus:
                    case OperatorType.Minus:
                    case OperatorType.Multiply:
                    case OperatorType.Divide:
                    case OperatorType.Modulus:
                    case OperatorType.Power:
                    case OperatorType.IntegerDivision:
                        return 2;
                    default:
                        return 0;
                }
            }
        }

        public int MinimalParametersCount
        {
            get
            {
                switch (type) {
                    case OperatorType.Plus:
                    case OperatorType.Minus:
                        return 1;
                    case OperatorType.Multiply:
                    case OperatorType.Divide:
                    case OperatorType.IntegerDivision:
                    case OperatorType.Modulus:
                    case OperatorType.Power:
                        return 2;
                    default:
                        return 0;
                }
            }
        }

        public double Calculate (params Operand [] operands)
        {
            switch (type) {
                case OperatorType.Plus:
                    if (operands.Length == 2)
                        return operands [0].Value + operands [1].Value;
                    if (operands.Length == 1)
                        return operands [0].Value;
                    throw new ExpressionErrorException ("Invalid number of operands");
                case OperatorType.Minus:
                    if (operands.Length == 2)
                        return operands [0].Value - operands [1].Value;
                    if (operands.Length == 1)
                        return -operands [0].Value;
                    throw new ExpressionErrorException ("Invalid number of operands");
                case OperatorType.Multiply:
                    if (operands.Length == 2)
                        return operands [0].Value * operands [1].Value;
                    throw new ExpressionErrorException ("Invalid number of operands");
                case OperatorType.Divide:
                    if (operands [1].Value == 0)
                        throw new ExpressionErrorException ("Division by zero");
                    if (operands.Length == 2)
                        return operands [0].Value / operands [1].Value;
                    throw new ExpressionErrorException ("Invalid number of operands");
                case OperatorType.IntegerDivision:
                    if (Math.Floor (operands [1].Value) == 0)
                        throw new ExpressionErrorException ("Division by zero");
                    if (operands.Length == 2)
                        return (int) Math.Floor (operands [0].Value) / (int) Math.Floor (operands [1].Value);
                    throw new ExpressionErrorException ("Invalid number of operands");
                case OperatorType.Modulus:
                    if (operands [1].Value == 0)
                        throw new ExpressionErrorException ("Division by zero");
                    if (operands.Length == 2)
                        return operands [0].Value % operands [1].Value;
                    throw new ExpressionErrorException ("Invalid number of operands");
                case OperatorType.Power:
                    if (operands.Length == 2)
                        return Math.Pow (operands [0].Value, operands [1].Value);
                    throw new ExpressionErrorException ("Invalid number of operands");
                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }

        public override string ToString ()
        {
            switch (type) {
                case OperatorType.Plus:
                    return "+";
                case OperatorType.Minus:
                    return "-";
                case OperatorType.Multiply:
                    return "*";
                case OperatorType.Divide:
                    return "/";
                case OperatorType.IntegerDivision:
                    return "\\";
                case OperatorType.Modulus:
                    return "%";
                case OperatorType.Power:
                    return "^";
                case OperatorType.OpenBrace:
                    return "(";
                case OperatorType.CloseBrace:
                    return ")";
                default:
                    return "operator";
            }
        }
    }
}
