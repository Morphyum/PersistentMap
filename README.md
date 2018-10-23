# WarTechServer
Client and Server for the persistent [WarTech](https://github.com/Morphyum/WarTech) map. Uses WCF to communicate via a RESTful API.

## Development
To bootstrap a local copy, you will need to do the following:

1. Download VS Community 2017 from https://visualstudio.microsoft.com/downloads/
2. Install ".NET desktop development" and "Game Development with Unity" packages in the VS Community 2017 installer
3. Open VS, open any .sln file under PersistentMap
4. Right click Solution, choose 'Restore nuGet packages'
5. Right click references, click Add Reference, add relevant DLL from steam\SteamApps\common\BATTLETECH\BattleTech_Data\Managed (names will match)
6. Set 'BattleTechGame' environment variable: Control Panel -> Search for 'environment' -> set to Steam path (as above, plus drive letter)
7. Enable your account to bind localhost:8001 `netsh http add urlacl url=http://+:8001/warServices user=<USERNAME>`

## TODOs
* Add Gzip compression on responses. Check that client can received
* Move IP flood check in WarServices::PostMissionResult to pre-request message inspector (http://mezhov.com/2008/05/data-compression-wcf/)
* "ServerURL" : "http://roguetech.org:8001/",