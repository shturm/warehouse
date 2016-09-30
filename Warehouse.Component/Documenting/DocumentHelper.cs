//
// DocumentHelper.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   04/04/2007
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
using System.Collections;

namespace Warehouse.Component.Documenting
{
    public static class DocumentHelper
    {
        #region Private fields

        private static IFormObjectCreator formObjectCreator = new FormObjectCreator ();
        private static IDrawingProvider drawingProvider;
        private static Hashtable variables = new Hashtable ();
        private static bool reallocatePageDependent;

        #endregion

        #region Public properties

        public static IFormObjectCreator FormObjectCreator
        {
            get { return formObjectCreator; }
            set { formObjectCreator = value; }
        }

        public static IDrawingProvider DrawingProvider
        {
            get
            {
                if (drawingProvider == null)
                    throw new Exception ("Drawing provider is not yet initialized.");

                return drawingProvider;
            }
            set { drawingProvider = value; }
        }

        public static PageSettings CurrentPageSettings { get; set; }

        public static Hashtable Variables
        {
            get { return variables; }
            set { variables = value; }
        }

        public static int CurrentPage
        {
            get
            {
                object ret = variables ["%CurrentPage%"];

                return ret == null ? 0 : (int) ret;
            }
            set
            {
                if (CurrentPage != value)
                    ReallocatePageDependent = true;

                variables ["%CurrentPage%"] = value;
            }
        }

        public static int TotalPages
        {
            get
            {
                object ret = variables ["%TotalPages%"];

                return ret == null ? 0 : (int) ret;
            }
            set
            {
                if (TotalPages != value)
                    ReallocatePageDependent = true;

                variables ["%TotalPages%"] = value;
            }
        }

        public static bool ReallocatePageDependent
        {
            get { return reallocatePageDependent; }
            set { reallocatePageDependent = value; }
        }

        #endregion
    }
}
