//
// DataReaderWrapper.cs
//
// Author:
//   Vladimir Dimitrov <vlad.dimitrov at gmail dot com>
//
// Created:
//   12/31/2007
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
using System.Data;

namespace Warehouse.Data
{
    internal abstract class DataReaderWrapper : IDataReader
    {
        private readonly object conn;
        private readonly TransactionContext tContext;
        private readonly IDataReader internalReader;
        private IDbCommand dbCommand;

        private bool isDisposed;

        public event EventHandler<DataReaderDisposedEventArgs> Disposed;

        public bool IsDisposed
        {
            get { return isDisposed; }
        }

        public DataReaderWrapper (object connection, TransactionContext context, IDataReader reader, IDbCommand dbCommand)
        {
            if (reader == null)
                throw new ArgumentNullException ("reader");

            conn = connection;
            tContext = context;
            internalReader = reader;
            this.dbCommand = dbCommand;
        }

        protected abstract DataReaderWrapper GetNew (object connection, TransactionContext context, IDataReader reader, IDbCommand dbCommand);

        protected virtual void TryCommand (Action command)
        {
            command ();
        }

        #region IDataReader Members

        public void Close ()
        {
            TryCommand (() => internalReader.Close ());
        }

        public DataTable GetSchemaTable ()
        {
            DataTable ret = null;

            TryCommand (delegate
                {
                    ret = internalReader.GetSchemaTable ();
                });
            return ret;
        }

        public bool NextResult ()
        {
            bool ret = false;

            TryCommand (delegate
                {
                    ret = internalReader.NextResult ();
                });
            return ret;
        }

        public bool Read ()
        {
            bool ret = false;

            TryCommand (delegate
                {
                    ret = internalReader.Read ();
                });
            return ret;
        }

        public int Depth
        {
            get
            {
                int ret = 0;

                TryCommand (delegate
                    {
                        ret = internalReader.Depth;
                    });
                return ret;
            }
        }

        public bool IsClosed
        {
            get
            {
                bool ret = false;

                TryCommand (delegate
                    {
                        ret = internalReader.IsClosed;
                    });
                return ret;
            }
        }

        public int RecordsAffected
        {
            get
            {
                int ret = 0;

                TryCommand (delegate
                    {
                        ret = internalReader.RecordsAffected;
                    });
                return ret;
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose ()
        {
            TryCommand (() =>
                {
                    if (!internalReader.IsClosed)
                        internalReader.Close ();

                    internalReader.Dispose ();
                });

            EventHandler<DataReaderDisposedEventArgs> onDisposed = Disposed;
            if (onDisposed == null || isDisposed)
                return;

            DataReaderDisposedEventArgs args = new DataReaderDisposedEventArgs (conn, tContext, dbCommand);
            dbCommand = null;
            onDisposed (this, args);
            isDisposed = true;
        }

        #endregion

        #region IDataRecord Members

        public string GetName (int i)
        {
            string ret = string.Empty;

            TryCommand (delegate
                {
                    ret = internalReader.GetName (i);
                });
            return ret;
        }

        public string GetDataTypeName (int i)
        {
            string ret = string.Empty;

            TryCommand (delegate
                {
                    ret = internalReader.GetDataTypeName (i);
                });
            return ret;
        }

        public Type GetFieldType (int i)
        {
            Type ret = null;

            TryCommand (delegate
                {
                    ret = internalReader.GetFieldType (i);
                });
            return ret;
        }

        public object GetValue (int i)
        {
            return internalReader.GetValue (i);
        }

        public int GetValues (object [] values)
        {
            return internalReader.GetValues (values);
        }

        public int GetOrdinal (string name)
        {
            int ret = 0;

            TryCommand (delegate
                {
                    ret = internalReader.GetOrdinal (name);
                });
            return ret;
        }

        public bool GetBoolean (int i)
        {
            return internalReader.GetBoolean (i);
        }

        public byte GetByte (int i)
        {
            return internalReader.GetByte (i);
        }

        public long GetBytes (int i, long fieldOffset, byte [] buffer, int bufferoffset, int length)
        {
            return internalReader.GetBytes (i, fieldOffset, buffer, bufferoffset, length);
        }

        public char GetChar (int i)
        {
            return internalReader.GetChar (i);
        }

        public long GetChars (int i, long fieldoffset, char [] buffer, int bufferoffset, int length)
        {
            return internalReader.GetChars (i, fieldoffset, buffer, bufferoffset, length);
        }

        public Guid GetGuid (int i)
        {
            return internalReader.GetGuid (i);
        }

        public short GetInt16 (int i)
        {
            return internalReader.GetInt16 (i);
        }

        public int GetInt32 (int i)
        {
            return internalReader.GetInt32 (i);
        }

        public long GetInt64 (int i)
        {
            return internalReader.GetInt64 (i);
        }

        public float GetFloat (int i)
        {
            return internalReader.GetFloat (i);
        }

        public double GetDouble (int i)
        {
            return internalReader.GetDouble (i);
        }

        public string GetString (int i)
        {
            return internalReader.GetString (i);
        }

        public decimal GetDecimal (int i)
        {
            return internalReader.GetDecimal (i);
        }

        public DateTime GetDateTime (int i)
        {
            return internalReader.GetDateTime (i);
        }

        public IDataReader GetData (int i)
        {
            IDataReader ret = null;

            TryCommand (delegate
                {
                    ret = GetNew (conn, tContext, internalReader.GetData (i), dbCommand);
                });
            return ret;
        }

        public bool IsDBNull (int i)
        {
            return internalReader.IsDBNull (i);
        }

        public int FieldCount
        {
            get
            {
                return internalReader.FieldCount;
            }
        }

        public object this [int i]
        {
            get
            {
                return internalReader [i];
            }
        }

        public object this [string name]
        {
            get
            {
                return internalReader [name];
            }
        }

        #endregion
    }
}
