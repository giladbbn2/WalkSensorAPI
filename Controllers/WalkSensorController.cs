using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WalkSensorAPI
{
    
    [ApiController]
    public class WalkSensorController : MainController
    {

        public const int MAX_MINUTES_SIGNATURE = 60;
        public const int MAX_FETCH_LIMIT = 30;
        public const int DEFAULT_FETCH_LIMIT = 3;



        public WalkSensorController(IConfiguration configuration) : base(configuration)
        {

        }

        [Route("api/walk/collect")]
        [HttpPost]
        public async Task Collect()
        {

            // lazy loading of mongodb client
            var sensorCollection = MongoDBHelper.SensorCollection;

            if (sensorCollection == null)
            {

                // log this!

                await SetErrorResponseJSONAsync("mongodb is not configured!");
                return;

            }

            var body = "";
            using (var reader = new StreamReader(HttpContext.Request.Body))
                body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
            {

                // log this!

                await SetErrorResponseJSONAsync("payload is empty");
                return;

            }

            InCollectDatapoints collect = null;

            try
            {

                collect = JsonConvert.DeserializeObject<InCollectDatapoints>(body);

                if (collect == null || string.IsNullOrEmpty(collect.device_id) || string.IsNullOrEmpty(collect.hmacmd5_digest) || string.IsNullOrEmpty(collect.hmacmd5_salt) || collect.datapoints.Length == 0)
                    throw new Exception("missing data in payload");

            }
            catch (Exception ex)
            {

                // log this!

                await SetErrorResponseJSONAsync($"exception: {ex.Message}, {ex.StackTrace}");
                return;

            }

            // check signature is still valid

            var currentUts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (currentUts - collect.message_uts > MAX_MINUTES_SIGNATURE)
            {

                // log this!

                await SetErrorResponseJSONAsync("signature not longer valid");
                return;

            }

            // verify authentication using hmac md5
            // we know in advance a 32 chars (guid) secret per device id
            // we just fetched it from our DB (or from a cache)
            // assuming all hashes and salt are lower case
            
            string deviceSecret = "a2bc1d3992300943a42181cbe814744c";

            body = body.Replace(collect.hmacmd5_digest, "").Replace(collect.hmacmd5_salt, "");

            var calculatedHash = StringHelper.MD5(body + "|" + deviceSecret + "|" + collect.hmacmd5_salt);

            if (collect.hmacmd5_digest != calculatedHash)
            {

                // log this!

                await SetErrorResponseJSONAsync("not authorized");
                return;

            }

            try
            {

                List<BsonDocument> docs = new List<BsonDocument>(collect.datapoints.Length);

                for (var i = 0; i < collect.datapoints.Length; i++)
                {

                    // we might get more than 3 datapoints due to internet not being available or otherwise
                    // we might get less than 3 datapoints?

                    // we don't insert negative values to db
                    if (collect.datapoints[i].distance < 0)
                    {

                        // log this!

                        // should we throw an exception here?

                        continue;

                    }
                    
                    // we save datetime in db only in UTC

                    var doc = new BsonDocument
                    {
                        { "device_id", collect.device_id },
                        { "startTime", new BsonDateTime(collect.datapoints[i].start_uts) },
                        { "endTime", new BsonDateTime(collect.datapoints[i].end_uts) },
                        { "distance", collect.datapoints[i].distance }
                    };

                    docs.Add(doc);

                }

                if (docs.Count == 0)
                {

                    // log this!

                    await SetErrorResponseJSONAsync("not a single valid datapoint");
                    return;

                }

                await sensorCollection.InsertManyAsync(docs);

            }
            catch (Exception ex)
            {

                // log this!

                await SetErrorResponseJSONAsync($"exception: {ex.Message}, {ex.StackTrace}");
                return;

            }

            await SetSuccessResponseJSONAsync("OK");

        }

        [Route("api/walk/fetch")]
        [HttpGet]
        public async Task Fetch([FromQuery] string device_id = null, [FromQuery] int? limit = 3, [FromQuery] int? offset = 0, [FromQuery] string starttime = null, [FromQuery] string endtime = null)
        {

            if (Request.Headers["x-api-key"].Count != 1 || Request.Headers["x-api-key"][0] != "PMAK-6096e117cd2e3500310e2835-39c18d49e9bc2473169b4a4dfe022a9324")
            {

                await SetErrorResponseJSONAsync("not authorized");
                return;

            }

            if (string.IsNullOrEmpty(device_id))
            {

                await SetErrorResponseJSONAsync("device_id cannot be empty");
                return;

            }

            // lazy loading of mongodb client
            var sensorCollection = MongoDBHelper.SensorCollection;

            if (sensorCollection == null)
            {

                // log this!

                await SetErrorResponseJSONAsync("mongodb is not configured!");
                return;

            }

            if (limit == null || limit < 0)
                limit = DEFAULT_FETCH_LIMIT;
            else if (limit > MAX_FETCH_LIMIT)
                limit = MAX_FETCH_LIMIT;

            if (offset == null || offset < 0)
                offset = 0;

            var output = new OutCollectDatapoints();

            output.device = device_id;
            output.points = new List<OutDatapoint>();

            var filterBuilder = Builders<BsonDocument>.Filter;

            List<FilterDefinition<BsonDocument>> filterDefinitions = new List<FilterDefinition<BsonDocument>>(3);

            filterDefinitions.Add(filterBuilder.Eq("device_id", device_id));

            if (starttime != null)
            {

                try
                {

                    // verify starttime is in RFC3339
                    
                    // ...

                    // parse into datetime
                    
                    var dt = DateTime.Parse(starttime).ToUniversalTime();

                    filterDefinitions.Add(filterBuilder.Gte("startTime", dt));

                }
                catch (Exception ex)
                {

                }

                try
                {

                    // verify endtime is in RFC3339

                    // ...

                    // parse into datetime

                    var dt = DateTime.Parse(endtime).ToUniversalTime();

                    filterDefinitions.Add(filterBuilder.Lt("endTime", dt));

                }
                catch (Exception ex)
                {

                }

            }

            var filter = filterBuilder.And(filterDefinitions);

            var sort = Builders<BsonDocument>.Sort.Descending("distance");

            // we don't need to get device_id from the docs in db
            var project = Builders<BsonDocument>.Projection.Include("startTime").Include("endTime").Include("distance");

            var task1 = sensorCollection.FindAsync(filter, new FindOptions<BsonDocument, BsonDocument>()
            {
                Sort = sort,
                Limit = limit,
                Skip = offset,
                Projection = project
            });

            // count total datapoints for this device

            var filterDeviceTotalPoints = filterBuilder.And(filterBuilder.Eq("device_id", device_id));

            var task2 = sensorCollection.CountDocumentsAsync(filterDeviceTotalPoints);

            // when task1 is completed all records are not necessarily fetched! (we get a cursor)
            await Task.WhenAll(task1, task2);

            output.totalPoints = await task2;

            using (var cursor = await task1)
            {

                // using ToList() allows the cursor to close faster, releasing db resources
                // but also requires us to iterate again over all retrieved docs (more cpu)
                // using ToList() is better for a small number of datapoints (what qualifies as "small" ?)

                //var docs = cursor.ToList();

                await cursor.ForEachAsync(doc =>
                {

                    try
                    {

                        var dist = doc["distance"].ToInt32();

                        // returned datetime in UTC in RFC3339

                        var start = doc["startTime"].ToUniversalTime();

                        var end = doc["endTime"].ToUniversalTime();

                        output.points.Add(new OutDatapoint()
                        {
                            distance = dist,
                            startTime = start,
                            endTime = end
                        });

                    }
                    catch (Exception ex)
                    {

                        // log this!

                    }

                });

            }

            Response.ContentType = "application/json";

            var bResponse = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(output));

            await Response.Body.WriteAsync(bResponse, 0, bResponse.Length);
        
        }

    }
}
