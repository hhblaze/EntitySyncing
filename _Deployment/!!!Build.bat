SET MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"

%MSBUILD_PATH% "%~dp0..\EntitySyncing.Net5\EntitySyncing.Net5.csproj" /t:rebuild /p:Configuration=Release
%MSBUILD_PATH% "%~dp0..\EntitySyncing.NetStandard20\EntitySyncing.NetStandard20.csproj" /t:rebuild /p:Configuration=Release

%MSBUILD_PATH% "%~dp0..\EntitySyncingClient.Net5\EntitySyncingClient.Net5.csproj" /t:rebuild /p:Configuration=Release
%MSBUILD_PATH% "%~dp0..\EntitySyncingClient.NetStandard20\EntitySyncingClient.NetStandard20.csproj" /t:rebuild /p:Configuration=Release

"%~dp0nuget.exe" pack "%~dp0EntitySyncing.nuspec" -BasePath "%~dp0.." -OutputDirectory "%~dp0..\_Deployment"
"%~dp0nuget.exe" pack "%~dp0EntitySyncingClient.nuspec" -BasePath "%~dp0.." -OutputDirectory "%~dp0..\_Deployment"