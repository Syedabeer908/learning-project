using StackExchange.Redis;

namespace WebApplication1.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _defaultTime;
        private readonly ILogger<RedisService> _logger;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _db = redis.GetDatabase();
            _defaultTime = TimeSpan.FromHours(1);
            _logger = logger;
        }

        private async Task<string> CreateKey(string prefix, Guid id)
        {
            var key = $"{prefix}:{id}";
            return key;
        }

        public async Task SetAsync(string prefix, Guid id, string value, TimeSpan? expiry = null)
        {
            try 
            {
                var key = await CreateKey(prefix, id);
                var expireTime = expiry ?? _defaultTime;
                await _db.StringSetAsync(key, value, expireTime);
            }
            catch
            {
                _logger.LogWarning("Redis Unavailable");
            }
            
        }
            
        public async Task<string?> GetAsync(string prefix, Guid id)
        {
            try
            {
                var key = await CreateKey(prefix, id);
                return await _db.StringGetAsync(key);
            }
            catch
            {
                _logger.LogWarning("Redis Unavailable");
                return null;
            }
        }

        public async Task RemoveAsync(string prefix, Guid id)
        {
            try
            {
                var key = await CreateKey(prefix, id);
                await _db.KeyDeleteAsync(key);

            }
            catch
            {
                _logger.LogWarning("Redis Unavailable");
            }
        }
    }
}
