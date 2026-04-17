using LanguageExt;
using McpServer.Application.Web;
using McpServer.Application.Web.Results;
using VapeCache.Abstractions.Caching;

namespace McpServer.Infrastructure.Caching
{
    public class VapeCacheWebResultCache : IWebResultCache
    {
        private readonly IVapeCache _cache;
        public VapeCacheWebResultCache(IVapeCache cache) => _cache = cache;

        public async ValueTask<Fin<FetchedPageResult?>> GetAsync(string key, CancellationToken ct)
        {
            var cacheKey = CacheKey<FetchedPageResult>.From(key);
            var result = await _cache.GetAsync(cacheKey, ct);
            return result == null ? Fin<FetchedPageResult?>.Succ(null) : Fin<FetchedPageResult?>.Succ(result);
        }

        public async ValueTask<Fin<Unit>> SetAsync(string key, FetchedPageResult value, TimeSpan ttl, CancellationToken ct)
        {
            var cacheKey = CacheKey<FetchedPageResult>.From(key);
            await _cache.SetAsync(cacheKey, value, new CacheEntryOptions(ttl), ct);
            return Fin<Unit>.Succ(Unit.Default);
        }
    }
}
