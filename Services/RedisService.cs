using StackExchange.Redis;

namespace WebApplication1.Services
{
    public class RedisService
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _defaultTime;

        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
            _defaultTime = TimeSpan.FromHours(1);
        }

        private async Task<string> CreateKey(string prefix, Guid id)
        {
            var key = $"{prefix}:{id}";
            return key;
        }

        public async Task SetAsync(string prefix, Guid id, string value, TimeSpan? expiry = null)
        {
            var key = await CreateKey(prefix, id);
            var expireTime = expiry ?? _defaultTime;
            await _db.StringSetAsync(key, value, expireTime);
        }

        public async Task<string?> GetAsync(string prefix, Guid id)
        {
            var key = await CreateKey(prefix, id);
            return await _db.StringGetAsync(key);
        }

        public async Task RemoveAsync(string prefix, Guid id)
        {
            var key = await CreateKey(prefix, id);
            await _db.KeyDeleteAsync(key);
        }
    }
}
