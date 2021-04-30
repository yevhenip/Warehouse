﻿using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Warehouse.Core.Interfaces.Repositories;
using Warehouse.Core.Settings;
using Warehouse.Domain;

namespace Warehouse.Api.Auth.Data
{
    public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(IMongoClient client, IOptions<PollySettings> pollySettings)
            : base(client.GetDatabase("Warehouse_auth").GetCollection<RefreshToken>("refreshTokens"), pollySettings.Value)
        {
        }
    }
}