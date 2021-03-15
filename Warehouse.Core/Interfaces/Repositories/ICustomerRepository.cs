﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Warehouse.Domain;

namespace Warehouse.Core.Interfaces.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();

        Task<Customer> GetAsync(string id);

        Task CreateAsync(Customer customer);

        Task UpdateAsync(Customer customer);

        Task DeleteAsync(string id);

        Task<List<Customer>> GetRangeAsync(IEnumerable<string> customerIds);
    }
}