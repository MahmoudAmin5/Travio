using Ardalis.Specification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Domain.Infrastructure.Contract
{
    public interface IGenericRepository<T> : IRepositoryBase<T> where T : class
    {
    }
}
