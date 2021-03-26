﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Warehouse.Core.Common;
using Warehouse.Core.DTO.Users;
using Warehouse.Domain;

namespace Warehouse.Core.Interfaces.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Gets all users
        /// </summary>
        /// <returns>List of users</returns>
        Task<Result<List<UserDto>>> GetAllAsync();
        
        /// <summary>
        /// Tries to get user firstly from cache, then from database, then from file. If user still null throws an exception
        /// </summary>
        /// <param name="id"></param>
        /// <returns>User</returns>
        Task<Result<UserDto>> GetAsync(string id);

        /// <summary>
        /// Creates, sets to cache, file system and sends a message with user
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Created user</returns>
        Task<Result<UserDto>> CreateAsync(UserDto user);

        /// <summary>
        /// Updates user in database, cache, filesystem and sends message with user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="user"></param>
        /// <returns>Updated user</returns>
        Task<Result<UserDto>> UpdateAsync(string userId, UserModelDto user);
        
        /// <summary>
        /// Updates user in database, cache and filesystem
        /// </summary>
        /// <param name="user"></param>
        Task UpdateAsync(User user);

        /// <summary>
        /// Deletes user from cache, file system, database and sends a message with user id
        /// </summary>
        /// <param name="id"></param>
        Task<Result<object>> DeleteAsync(string id);
    }
}