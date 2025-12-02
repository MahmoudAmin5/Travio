using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Infrastructure.Repositories
{
    public class GenericRepository<T> : RepositoryBase<T> where T : class
    {
        public GenericRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }
    }
}
