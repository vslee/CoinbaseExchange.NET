using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinbaseExchange.NET.Core
{
    public abstract class ExchangePageableRequestBase : ExchangeRequestBase
    {
        public RequestPaginationType PaginationType { get; protected set; }
        public Int16 Cursor { get; protected set; }
        public byte? RecordCount { get; protected set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="method"></param>
		/// <param name="cursor"></param>
		/// <param name="recordCount">default is 100 per GDAX documentation</param>
        public ExchangePageableRequestBase(string method, Int16 cursor = 0, byte? recordCount = null) : base(method)
        {
			this.Cursor = cursor;
			this.RecordCount = recordCount;
        }
    }
}
