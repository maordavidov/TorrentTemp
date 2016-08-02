using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBProxy
{
    using System.Threading;

    using Model;

    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;

    public class TorrentCollectionProxy
    {
        private readonly IMongoCollection<TorrentInfo> _torrentCollection;

        static TorrentCollectionProxy()
        {
            BsonClassMap.RegisterClassMap<TorrentInfo>(cm => {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        public TorrentCollectionProxy()
        {
          
            IMongoDatabase mongoDatabase = new MongoClient().GetDatabase("AutoTorrentDownloader");
            _torrentCollection = mongoDatabase.GetCollection<TorrentInfo>("Torrent");
            var nameKey = Builders<TorrentInfo>.IndexKeys.Hashed(nameof(TorrentInfo.TorrentName));
            var imdbHashKey = Builders<TorrentInfo>.IndexKeys.Hashed(nameof(TorrentInfo.ImdbHash));
            _torrentCollection.Indexes.CreateOne(nameKey);
            _torrentCollection.Indexes.CreateOne(imdbHashKey);
        }

        public IMongoCollection<TorrentInfo> GetMongoCollection()
        {
            return _torrentCollection;
        }

        public Task InsertManyAsync(IEnumerable<TorrentInfo> torrentInfos)
        {
            return Task.Run(() => _torrentCollection.InsertMany(torrentInfos));
        }

        public async Task InsertOneAsync(TorrentInfo torrentInfo)
        {
            await Task.FromResult(0);
            _torrentCollection.InsertOne(torrentInfo);
        }

    }
}
