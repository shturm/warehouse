//
// ReceiptDetail.cs
//
// Author:
//   Vladimir Dimitrov (vlad.dimitrov at gmail dot com)
//
// Created:
//   07/27/2006
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
using Warehouse.Business.Documenting;
using Warehouse.Business.Operations;

namespace Warehouse.Business.Entities
{
    public class ReceiptDetail : DocumentDetail
    {
        #region Private members

        private string oldQuantity = string.Empty;
        private string multiply = "x";
        private string equal = "=";
        private string newModLabel = string.Empty;
        private string oldModLabel = string.Empty;
        private readonly List<string> oldModifiers = new List<string> ();
        private readonly List<string> modifiers = new List<string> ();

        #endregion

        #region Public properties

        [FormMemberMapping ("OldQuantity")]
        public string OldQuantity
        {
            get { return oldQuantity; }
            set { oldQuantity = value; }
        }

        [FormMemberMapping ("Multiply")]
        public string Multiply
        {
            get { return multiply; }
            set { multiply = value; }
        }

        /// <summary>
        /// Gets the amount of bank notes (the whole part) of the price of this <see cref="ReceiptDetail"/>.
        /// </summary>
        [FormMemberMapping ("priceBankNotes")]
        public string PriceBankNotes
        {
            get
            {
                return Entities.Currency.BankNotesString (Entities.Currency.ParseExpression (Price, TotalsPriceType));
            }
        }

        /// <summary>
        /// Gets the amount of coins (the fractional part) of the price of this <see cref="ReceiptDetail"/>.
        /// </summary>
        [FormMemberMapping ("priceCoins")]
        public string PriceCoins
        {
            get
            {
                return Entities.Currency.CoinsString (Entities.Currency.ParseExpression (Price, TotalsPriceType));
            }
        }

        [FormMemberMapping ("Equal")]
        public string Equal
        {
            get { return equal; }
            set { equal = value; }
        }

        /// <summary>
        /// Gets the amount of bank notes (the whole part) of the total price of this <see cref="ReceiptDetail"/>.
        /// </summary>
        [FormMemberMapping ("totalBankNotes")]
        public string TotalBankNotes
        {
            get
            {
                return Entities.Currency.ToString (Math.Floor (Entities.Currency.ParseExpression (Total, TotalsPriceType)), 0);
            }
        }

        /// <summary>
        /// Gets the amount of coins (the fractional part) of the total price of this <see cref="ReceiptDetail"/>.
        /// </summary>
        [FormMemberMapping ("totalCoins")]
        public string TotalCoins
        {
            get
            {
                double priceValue = Entities.Currency.ParseExpression (Total, TotalsPriceType);
                return Entities.Currency.ToString ((priceValue - Math.Floor (priceValue)) * 100, 0);
            }
        }

        [FormMemberMapping ("NewModLabel")]
        public string NewModLabel
        {
            get { return newModLabel; }
            set { newModLabel = value; }
        }

        [FormMemberMapping ("OldModifiers")]
        public List<string> OldModifiers
        {
            get { return oldModifiers; }
        }

        [FormMemberMapping ("OldModLabel")]
        public string OldModLabel
        {
            get { return oldModLabel; }
            set { oldModLabel = value; }
        }

        [FormMemberMapping ("Modifiers")]
        public List<string> Modifiers
        {
            get { return modifiers; }
        }

        #endregion

        public ReceiptDetail ()
        {
        }

        public ReceiptDetail (int detailNumber, OperationDetail detail, bool usePriceIn = true)
            : base (detailNumber, detail, usePriceIn)
        {
        }
    }
}