using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Common.Constants;
using WebApplication1.Common.Exceptions;
using WebApplication1.Common.Results;
using WebApplication1.DTOs;
using WebApplication1.Entities;
using WebApplication1.Interfaces;
using WebApplication1.Mappers;

namespace WebApplication1.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly RedisService _redisServcie;
        private readonly AuthConstants _authConstants;
        private readonly RoleConstants _roleConstants;
        private readonly AuthMapper _mapper;
        private readonly PasswordHasher<User> _hasher;

        public AuthService(IUserRepository userRepo, IRoleRepository roleRepo,
            IRefreshTokenRepository refreshTokenRepo, RedisService redisServcie,
            IOptions<AuthConstants> authOptions, IOptions<RoleConstants> rolesettings)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _redisServcie = redisServcie;
            _authConstants = authOptions.Value;
            _roleConstants = rolesettings.Value;
            _mapper = new AuthMapper();
            _hasher = new PasswordHasher<User>();
        }

        private async Task CheckEmailUnique(string email, Guid? excludeId = null)
        {
            var exists = await _userRepo.EmailExistsAsync(email, excludeId);

            if (exists)
                throw new BadRequestException($"Invalid Email '{email}'.");
        }

        private async Task ValidatePassword(User user, string password)
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new BadRequestException("Invalid email or password");
            }
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, password);
                await _userRepo.UpdateAsync(user);
            }
        }

        private string GenerateToken(User user)
        {
            var secretKey = _authConstants.Key;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim(ClaimTypes.Version, user.TokenVersion.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_authConstants.ExpiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
       
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes);
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async Task<ResultT<AuthResponseDto>> IssueTokens(User user, Guid? tokenId = null, Guid? familyId = null)
        {
            var accessToken = GenerateToken(user);
            var refreshToken = GenerateRefreshToken();

            var entity = _mapper.ToRefreshTokenEntity(refreshToken, user.UserId);

            if (familyId != null && tokenId != null) 
            {
                entity.FamilyId = familyId.Value;
                entity.ReplacedByTokenId = tokenId.Value;
            }

            entity.Token = HashToken(refreshToken);

            await _refreshTokenRepo.AddAsync(entity);

            return ResultT<AuthResponseDto>.Success(
                _mapper.ToDto(accessToken, refreshToken)
            );
        }

        private async Task<RefreshToken> CheckRefreshTokenIsValid(string token)
        {
            if (await _redisServcie.ExistAsync("revoked", token))
                throw new BadRequestException("Invalid refresh token");

            var hashedToken = HashToken(token);
            var storedToken = await _refreshTokenRepo.GetByTokenAsync(hashedToken);

            if (storedToken == null)
            {
                await _redisServcie.SetAsync("revoked", token, "1", TimeSpan.FromMinutes(5));
                throw new BadRequestException("Invalid refresh token");
            }

            if(storedToken.ExpiryDate < DateTime.UtcNow)
                throw new BadRequestException("Invalid refresh token");

            if (storedToken.IsRevoked)
            {
                await RevokeAllTokensInFamily(storedToken.FamilyId);
                throw new ForbiddenException("Token reuse detected");
            }
            return storedToken;
        }

        public async Task RevokeAllTokensInFamily(Guid familyId)
        {
            var tokens = await _refreshTokenRepo.GetByFamilyIdAsync(familyId);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _refreshTokenRepo.UpdateRangeAsync(tokens);
        }

        public async Task RevokeAllTokensOfUser(Guid userId)
        {
            var tokens = await _refreshTokenRepo.GetByUserIdAsync(userId);

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
            }

            await _refreshTokenRepo.UpdateRangeAsync(tokens);
        }

        private async Task<User> CheckUserExistAndGet(Guid userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);

            if (user == null)
                throw new BadRequestException("User not found");
            return user;
        }

        public async Task<ResultT<AuthResponseDto>> RegisterAsync(AuthRegisterDto dto)
        {
            await CheckEmailUnique(dto.Email);
            var role = await _roleRepo.GetByNameAsync(_roleConstants.User);

            var user = _mapper.ToEntity(dto, role);

            await _userRepo.AddAsync(user);

            return await IssueTokens(user);
        }

        public async Task<ResultT<AuthResponseDto>> LoginAsync(AuthLoginDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email);

            if (user == null || user.IsDeleted)
                throw new NotFoundException("Invalid email or password");

            await ValidatePassword(user, dto.Password);

            await RevokeAllTokensOfUser(user.UserId);

            return await IssueTokens(user);
        }

        public async Task<ResultT<AuthResponseDto>> Refresh(RefreshTokenDto request)
        {
            var storedToken = await CheckRefreshTokenIsValid(request.Token);
            var user = await CheckUserExistAndGet(storedToken.UserId);

            storedToken.IsRevoked = true;

            await _refreshTokenRepo.UpdateAsync(storedToken);

            await _redisServcie.SetAsync("revoked", request.Token, "1", TimeSpan.FromDays(7));

            return await IssueTokens(user, storedToken.RefreshTokenId, storedToken.FamilyId);
        }
    }
}
