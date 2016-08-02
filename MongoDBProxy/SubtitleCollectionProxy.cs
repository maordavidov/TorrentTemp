using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBProxy
{
    using Model;

    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;

    public class SubtitleCollectionProxy
    {
        private readonly IMongoCollection<SubtitleInfo> _subtitlesCollection;

        static SubtitleCollectionProxy()
        {
            BsonClassMap.RegisterClassMap<SubtitleInfo>(cm => {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        public SubtitleCollectionProxy()
        {
            IMongoDatabase mongoDatabase = new MongoClient().GetDatabase("AutoTorrentDownloader");
            _subtitlesCollection = mongoDatabase.GetCollection<SubtitleInfo>("Subtitles");
            var nameKey = Builders<SubtitleInfo>.IndexKeys.Hashed(nameof(SubtitleInfo.Name));
            var imdbHashKey = Builders<SubtitleInfo>.IndexKeys.Hashed(nameof(SubtitleInfo.ImdbHash));
            _subtitlesCollection.Indexes.CreateOne(nameKey);
            _subtitlesCollection.Indexes.CreateOne(imdbHashKey);
        }

        public IMongoCollection<SubtitleInfo> GetMongoCollection()
        {
            return _subtitlesCollection;
        }

        public async Task InsertOneAsync(SubtitleInfo subtitleInfo)
        {
            await Task.FromResult(0);
            _subtitlesCollection.InsertOne(subtitleInfo);
        }

        public Task<bool> AnyAsync(VideoInfo videoInfo)
        {
            var filterDefinition = Builders<SubtitleInfo>.Filter.Eq(f => f.ImdbHash, videoInfo.IMDBInfo.Hash);
            
            return Task.Run(
                () =>
                {
                    var asyncCursor = _subtitlesCollection.Find(filterDefinition);
                    return asyncCursor.Any() == true;
                });

        }

        public Task InsertManyAsync(IEnumerable<SubtitleInfo> subtitleInfos)
        {
            return _subtitlesCollection.InsertManyAsync(subtitleInfos);
        }
    }
}
