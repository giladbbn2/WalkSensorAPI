using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WalkSensorAPI
{
    public static class MongoDBHelper
    {

        public static IConfiguration Configuration { get; set; }

        private static MongoClient _client;

        private static IMongoCollection<BsonDocument> _sensorCollection;

        public static MongoClient Client
        {
            get
            {

                if (_client != null)
                    return _client;

                var mongoDBConnectionString = Configuration.GetSection("MongoDBConnectionString")?.Value;

                if (string.IsNullOrEmpty(mongoDBConnectionString))
                    return null;

                try
                {

                    _client = new MongoClient(mongoDBConnectionString);

                }
                catch (Exception ex)
                {

                    // log this exception

                    return null;

                }

                return _client;

            }
        }

        public static IMongoCollection<BsonDocument> SensorCollection
        {
            get
            {

                if (_sensorCollection != null)
                    return _sensorCollection;

                var client = Client;

                if (client == null)
                    return null;

                try
                {

                    var database = client.GetDatabase("WalkSensor");

                    _sensorCollection = database.GetCollection<BsonDocument>("sensor");

                }
                catch (Exception ex)
                {

                    // log this exception

                    return null;

                }

                return _sensorCollection;

            }
        }

    }
}
