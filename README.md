# WalkSensorAPI v1.x

This is a possible solution for the AposHealth R&D Manager Assignment.
In this task I chose to include a docker compose file.

I've decided to use MongoDB for saving all time series datapoints.
The db is called "WalkSensor", having a collection called "sensor".
This is a regular collection and not a time series collection.

The document structure is:
{
	_id: ObjectId("..."),
	device_id: "541f284102554d1897bbe4ac60fc1d84",
	startTime: "2021-07-31T22:38:39.000+00:00",
	endTime: "2021-07-31T22:38:40.000+00:00",
	distance: 78
}

For performance I've created a compound index: [device_id: 1, distance: -1] 


__Assumptions:__

There will be many of IoT devices (or brokers, i.e. mediating mobile devices)
sending mostly time series data to our servers. At the moment the only measure we record is
the distance in meters. Lowest granularity recorded is a time interval of 20 mins, for which
the total distance is summed up.

In this solution I do not presume HOW the data is sent to the web server. In general there
are two approaches to data collection from IoT devices: connecting directly to the cloud
or collection by a mediator - for example a mobile phone app that syncs with the IoT device, 
accumulates the data and sends it securely to the web server.
For this task I assume the IoT device sends the data directly to the server.

I assume each IoT hardware is hardcoded with two random 128 bits. One is the device_id
and the second is the device_secret. Both known to the server. The security model
doesn't encrypt the transmitted data but only enables authentication of the sending
party by means of md5 hmac.
It is possible to use a symmetric encryption (e.g. AES256) for both encryption of the
transmitted data and for authentication of the sender. It is also possible to
use a public-private key scheme for both as well.


__HOW DO I RUN THIS:__

For the purpose of testing a fresh vm with ubuntu 20.04 was created, for which ufw was 
disabled. docker was installed using the convenience script here:

https://get.docker.com/

docker compose:

https://docs.docker.com/compose/install/

dotnet5 sdk:

https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#2004

__IMPORTANT NOTICE: After executing "docker-compose up" for the first time, MongoDB Compass was used
to connect to the db server (listening on {IP}:27017) and set up a new DB with the name 
"WalkSensor" with a collection called "sensor".__

For testing purposes a simple html page is served at http://{IP}


## DB Research Question:

Choosing the right DB (or databases, in plural) is one of the biggest architectural concerns, 
especially when creating a new system from the ground up. It requires an in-depth analysis of 
the product demands and the technological requirements as well as a deep understanding in
various database systems.

I made a list of every aspect important for me with regard to selecting a DB and divided it into three:

__Capabilities and restrictions:__

1. Indexing mechanism - which algorithms are supported and which are not (b-tree, lucene, r-tree)? do
the indexes allow intersections? what are the restrictions of these indexes?

2. Data structures - relational tables (sql-like), key-value (redis, memcache), sets and sorted sets (redis),
graph relationships (neo4j).

3. Optimal storage media - redis (ram), redis on flash (ram + ssd), sql-like (ram + ssd + hdd), presto (object)

4. General purpose, versatility - suitable for OLTP, OLAP or both? time-series AND relational?

5. Optimal for time-series - fast data ingest, compression, benefits from immutability

6. Optimal with regard to infinite cardinality

7. Distribution solution type: master-slave with failover, master-master, sharded

8. Ease of deployment - adding another replica or shard, when necessary

9. Ease of migration and backup

10. Allows partitioning

11. Consistency model: ACID vs BASE

12. Concurrent connections to db restrictions

13. Stability - tendancy to shutdown, error on startup, data corruption 

__Community:__

1. Battle tested - case studies of big companies

2. Large and active community - more info online

3. Hiring force - how well-known within the developers community

4. Being actively developed and having a future roadmap

__Extendibility:__

1. Ease of deployment in Kubernetes

2. Has working IDE

3. Easy integrations to ML tools

4. High Availability out-of-the-box simple tools for scaling, replication, sharding and routing

5. Available as a managed solution on major cloud providers (at what cost?)

6. Connectors maturity and connection pooling

7. Learning curve steepness

8. Strictness of data structure - documents vs sql tables

In addition to defining the different aspects of comparison between DBs we also must define our product and technology needs.
I listed a partial list of question we should be asking ourselves:
1. How many clients will request our servers? Get estimated rates
2. Are we legally obligated to use a cloud provider? Data ingress/outgress has additional costs.
3. Do we have a budget for a managed db solution? What is the risk? what is the SLA?
4. Can we project the increase in the amount of IoT devices over time?
5. Are the requests governed by seasonality? how often would there be high peaks in rps?
6. What is the estimated size of data accumulated per year (in GB)?
7. Will our clients be satisfied with a response of several seconds or should the response be immediate?
8. Is the data mostly immutable?
9. Can we define the frequent questions our clients will ask in order to provide a better performing algorithm?
For instance, we can save aggregations of highly-granular time ranges (weeks, months). i.e. should we save datapoints to S3 or to MongoDB?



For our best candidates we will have to do load testing or at least get estimates for:
1. minimum cpu/ram requirements
2. max insert rate
3. concurrency cpu load
4. max concurrent connections
5. read/write time with and without index (when db is very much full)
6. aggregation performance
7. sharding and backup difficulty

Based on the exercise I was given our datapoints are in a time-series and immutable. Our data is not relational in nature.
Because we need to fetch the datapoints sorted by distance and NOT by datetime, we need to define an index.
The fact that our data is in a time-series removed relational DBs from the plate, which can't operate on billions of records anyway.

Possible candidates are: mongodb, sqlite, aws s3 athena, aws s3 select, presto on aws s3.

Less popular candidates: mysql ndb, riak, druid, influxdb, aiven

A well-known, with a very large community DB capable of holding vast amounts of records is mongodb, which also has an out-of-the-box sharding and replication mechanisms.
It is also provided as a managed solution on major cloud providers and it is rising in popularity, so it won't become obselete any time soon.
Benchmarks show it is very fast for inserts compared with sql-like solutions, but they also state mongodb is not very fast for reads when compared to other solutions.
For the sole purpose of this exercise mongodb was chosen as a good DB solution, but in reality we would load test ourselves and won't rely only on benchmarks.
Moreover, we will understand the product demands in much more detail (like the questions I wrote above) and come up with several candidates that we would test further.
Then, after taking into account all aspects written above (capabilities and restrictions, community, extendibility), we will be able to suggest the best candidate suited for our needs.

