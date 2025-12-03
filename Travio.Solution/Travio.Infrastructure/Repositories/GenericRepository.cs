using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Infrastructure.Contract;

namespace Travio.Infrastructure.Repositories
{
    public class GenericRepository<T> : RepositoryBase<T>,IGenericRepository<T> where T : class
    {
        public GenericRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
