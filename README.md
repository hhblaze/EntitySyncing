# EntitySyncing

Synchronizes entities between server and clients, using DBreeze techniques (C# Xamarin).

Typical case is a mobile application (APP) that wants to have in a local database list of tasks of the concrete user.
Local database gives an ability to read and create tasks being offline from the server.

User can install such APP on several own mobile devices.

EntitySyncingServer nuget package must be installed on the server, EntitySyncingClient must be installed on the client
and both must be configured.

The transfer data mechanizm is not implemented in this project (let'S imagine that we have already open tcp/http channel between clients and the server), 
there are examples how to supply incoming data from the client to server and which data to supply back and run it on the client.



