﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoTutorial.Core.Common;
using MongoTutorial.Core.DTO.Auth;
using MongoTutorial.Core.DTO.Users;
using MongoTutorial.Core.Interfaces.Repositories;
using MongoTutorial.Core.Interfaces.Services;
using MongoTutorial.Core.Settings;
using MongoTutorial.Domain;

namespace MongoTutorial.Business.Services
{
    public class AuthService : ServiceBase<RefreshToken>, IAuthService
    {
        private readonly JwtTokenConfiguration _tokenConfiguration;
        private readonly IRefreshTokenRepository _tokenRepository;
        private readonly PasswordHasher<UserDto> _hasher;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public AuthService(IOptions<JwtTokenConfiguration> tokenConfiguration, IRefreshTokenRepository tokenRepository,
            IUserService userService, IMapper mapper)
        {
            _tokenConfiguration = tokenConfiguration.Value;
            _tokenRepository = tokenRepository;
            _userService = userService;
            _mapper = mapper;
            _hasher = new();
        }

        public async Task<Result<UserDto>> RegisterAsync(RegisterDto register)
        {
            var user = _mapper.Map<UserDto>(register);
            var hashedPassword = _hasher.HashPassword(user, register.Password);
            user = user with {PasswordHash = hashedPassword, Roles = new List<string> {"User"}};
            await _userService.CreateAsync(user);
            return Result<UserDto>.Success(user);
        }

        public async Task<Result<UserAuthenticatedDto>> LoginAsync(LoginDto login)
        {
            var user = (await _userService.GetByUserNameAsync(login.UserName)).Data;
            IsValid(user, login.Password);

            var jwtToken = await GenerateJwtToken(user);
            var tokenString = await GenerateRefreshToken();
            var userInDb = _mapper.Map<User>(user);
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTime.UtcNow,
                DateExpires = DateTime.UtcNow.AddMinutes(_tokenConfiguration.RefreshTokenExpirationMinutes),
                Token = tokenString,
                User = userInDb
            };

            await _tokenRepository.CreateAsync(refreshToken);
            UserAuthenticatedDto authenticatedDto = new(user, jwtToken, refreshToken.Token);
            return Result<UserAuthenticatedDto>.Success(authenticatedDto);
        }

        public async Task<Result<UserAuthenticatedDto>> RefreshTokenAsync(string userId, TokenDto token)
        {
            var user = (await _userService.GetAsync(userId)).Data;
            var refreshTokenInDb = await _tokenRepository.GetAsync(userId, token.Name);

            CheckForNull(refreshTokenInDb);
            IsValid(refreshTokenInDb);

            var jwtToken = await GenerateJwtToken(user);
            var tokenString = await GenerateRefreshToken();
            var userInDb = _mapper.Map<User>(user);
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid().ToString(),
                DateCreated = DateTime.UtcNow,
                DateExpires = DateTime.UtcNow.AddMinutes(_tokenConfiguration.RefreshTokenExpirationMinutes),
                Token = tokenString,
                User = userInDb
            };

            await _tokenRepository.CreateAsync(refreshToken);
            await _tokenRepository.DeleteAsync(refreshTokenInDb.Id);
            UserAuthenticatedDto authenticatedDto = new(user, jwtToken, refreshToken.Token);
            return Result<UserAuthenticatedDto>.Success(authenticatedDto);
        }

        private void IsValid(UserDto user, string password)
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result is PasswordVerificationResult.Failed)
            {
                throw Result<UserAuthenticatedDto>.Failure("password", "Invalid password");
            }
        }

        private static void IsValid(RefreshToken token)
        {
            if (token.DateExpires <= DateTime.UtcNow)
            {
                throw Result<RefreshToken>.Failure("token", "Token is expired");
            }
        }
        
        private Task<string> GenerateJwtToken(UserDto user)
        {
            List<Claim> claims = new()
            {
                new("Id", user.Id),
                new("UserName", user.UserName),
                new("Email", user.Email)
            };
            claims.AddRange(user.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var jwtToken = new JwtSecurityToken(
                _tokenConfiguration.Issuer,
                _tokenConfiguration.Audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(_tokenConfiguration.AccessTokenExpirationMinutes),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenConfiguration.Secret)),
                    SecurityAlgorithms.HmacSha256Signature));

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(jwtToken));
        }

        private static Task<string> GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Task.FromResult(Convert.ToBase64String(randomNumber));
        }
    }
}