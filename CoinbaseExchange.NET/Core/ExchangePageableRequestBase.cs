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
        //public Int16 ZeroBasedCursor { get; protected set; } (GDAX doesn't use int cursors anymore)
        public int? RecordCount { get; protected set; }

		public string afterCursor { get; protected set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="method"></param>
		/// <param name="zeroBaseCursor"></param>
		/// <param name="recordCount">default and max is 100 per GDAX documentation</param>
		public ExchangePageableRequestBase(string method, string afterCursor = null, int? recordCount = 100) : base(method)
        {
			this.afterCursor = afterCursor;
			this.RecordCount = recordCount;
        }
    }
}
