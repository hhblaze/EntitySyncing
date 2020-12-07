# EntitySyncing

[![NuGet Badge](https://buildstats.info/nuget/EntitySyncingServer)](https://www.nuget.org/packages/EntitySyncingServer/)
[![NuGet Badge](https://buildstats.info/nuget/EntitySyncingClient)](https://www.nuget.org/packages/EntitySyncingClient/)
[![Image of Build](https://img.shields.io/badge/Powered%20by-tiesky.com-1883F5.svg)](https://tiesky.com)

Synchronizes entities between the server and the clients, using <a href = 'https://github.com/hhblaze/DBreeze/'  target='_blank'>DBreeze</a> techniques (made for .NET C# Xamarin Core Stabdard).

Typical case is a mobile application (APP) that wants to have in a local database a list of TODO-tasks for the concrete user.
Local database gives an ability to read and create tasks being offline from the server.

User can install such APP on several own mobile devices.

<a href = 'https://www.nuget.org/packages/EntitySyncingServer/'  target='_blank'>EntitySyncingServer</a>  nuget package must be installed on the server, <a href = 'https://www.nuget.org/packages/EntitySyncingClient/'  target='_blank'>EntitySyncingClient</a> must be installed on the client
and both must be configured.

The transfer data mechanizm is not implemented in this project (let's imagine that there is an open tcp/http channel between clients and the server), 
there are <a href = 'https://github.com/hhblaze/EntitySyncing/tree/main/EntitySyncingClientTester/'  target='_blank'>examples</a> how to supply incoming data from the client to server and which data to supply back and run it on the client.

Synchronization is always initiated from the client.



It's a free software for those who think that it should be free.

hhblaze@gmail.com

