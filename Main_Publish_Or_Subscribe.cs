
String accessToken = "{accessToken}";
String natsUrl = "{nats://url}";
String streamName = "{streamName}";

NatsProvider natsProvider = new NatsProvider(accessToken, natsUrl, streamName);
natsProvider.Connect();

/**
* if you want to subscribe messages*

natsProvider.SubscribeSync();*/





/**
* if you want to publish something:
**/

string msg = "Hello World";
natsProvider.Publish(msg);