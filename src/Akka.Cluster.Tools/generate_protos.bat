setlocal

@rem enter this directory
cd /d %~dp0

set PROTOC=%UserProfile%\.nuget\packages\Google.Protobuf.Tools\3.5.1\tools\windows_x64\protoc.exe
set PROTOPATH=%UserProfile%\.nuget\packages\Google.Protobuf.Tools\3.5.1\tools\

%PROTOC% PublishSubscribe/Serialization/Proto/DistributedPubSubMessages.proto -I. -I.. --csharp_out=PublishSubscribe/Serialization/Proto   --csharp_opt=file_extension=.g.cs
%PROTOC% Client/Serialization/Proto/ClusterClientMessages.proto -I. --csharp_out=Client/Serialization/Proto   --csharp_opt=file_extension=.g.cs

endlocal
