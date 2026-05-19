using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Contracts.Services.CurruncyExchange
{
    public interface ICurrencyExchangeService 
    {
        Task<decimal> GetExchangeRateAsync(string baseCurrency, string targetCurrency, CancellationToken ct = default);
    }
}
