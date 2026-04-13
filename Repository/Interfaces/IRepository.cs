using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication1.Repository.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<List<T>> GetAllAsync();
        Task<T?> GetByIdAsync(Guid id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<bool> CheckIfExist(int id);
    }
}
