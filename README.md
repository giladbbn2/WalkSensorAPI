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


__Testing:__

For the purpose of testing a fresh vm with ubuntu 20.04 was created, for which ufw was 
disabled. docker was installed using the convenience script here:
https://get.docker.com/
docker compose:
https://docs.docker.com/compose/install/
dotnet5 sdk:
https://docs.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#2004

After executing "docker-compose up" for the first time, MongoDB Compass was used
to connect to the db server (listening on {IP}:27017) and set up a new DB with the name 
"WalkSensor" with a collection called "sensor".

For testing purposes a simple html page is served at http://{IP}
