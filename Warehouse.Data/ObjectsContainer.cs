//
// ObjectsContainer.cs
//
// Author:
//   Vladimir Dimitrov <vdimitrov at vladster dot net>
//
// Created:
//   02.13.2014
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

namespace Warehouse.Data
{
    public class ObjectsContainer<T1, T2>
    {
        [DbColumn ("Value1")]
        public T1 Value1 { get; set; }

        [DbColumn ("Value2")]
        public T2 Value2 { get; set; }

        public ObjectsContainer ()
        {
        }

        public ObjectsContainer (T1 v1, T2 v2)
        {
            Value1 = v1;
            Value2 = v2;
        }

        public override string ToString ()
        {
            return string.Format ("V1: {0}, V2: {1}", Value1, Value2);
        }
    }

    public class ObjectsContainer<T1, T2, T3> : ObjectsContainer<T1, T2>
    {
        [DbColumn ("Value3")]
        public T3 Value3 { get; set; }

        public ObjectsContainer ()
        {
        }

        public ObjectsContainer (T1 v1, T2 v2, T3 v3)
            : base (v1, v2)
        {
            Value3 = v3;
        }

        public override string ToString ()
        {
            return base.ToString () + ", V3: " + Value3;
        }
    }

    public class ObjectsContainer<T1, T2, T3, T4> : ObjectsContainer<T1, T2, T3>
    {
        [DbColumn ("Value4")]
        public T4 Value4 { get; set; }

        public ObjectsContainer ()
        {
        }

        public ObjectsContainer (T1 v1, T2 v2, T3 v3, T4 v4)
            : base (v1, v2, v3)
        {
            Value4 = v4;
        }

        public override string ToString ()
        {
            return base.ToString () + ", V4: " + Value4;
        }
    }

    public class ObjectsContainer<T1, T2, T3, T4, T5> : ObjectsContainer<T1, T2, T3, T4>
    {
        [DbColumn ("Value5")]
        public T5 Value5 { get; set; }

        public ObjectsContainer ()
        {
        }

        public ObjectsContainer (T1 v1, T2 v2, T3 v3, T4 v4, T5 v5)
            : base (v1, v2, v3, v4)
        {
            Value5 = v5;
        }

        public override string ToString ()
        {
            return base.ToString () + ", V5: " + Value5;
        }
    }

    public class ObjectsContainer<T1, T2, T3, T4, T5, T6> : ObjectsContainer<T1, T2, T3, T4, T5>
    {
        [DbColumn ("Value6")]
        public T6 Value6 { get; set; }

        public ObjectsContainer ()
        {
        }

        public ObjectsContainer (T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6)
            : base (v1, v2, v3, v4, v5)
        {
            Value6 = v6;
        }

        public override string ToString ()
        {
            return base.ToString () + ", V6: " + Value6;
        }
    }
}
