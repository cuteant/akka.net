setlocal

@rem enter this directory
cd /d %~dp0

set PROTOC=%UserProfile%\.nuget\packages\Google.Protobuf.Tools\3.5.1\tools\windows_x64\protoc.exe
set PROTOPATH=%UserProfile%\.nuget\packages\Google.Protobuf.Tools\3.5.1\tools\

%PROTOC% Proto/TestConductorProtocol.proto -I. -I.. --csharp_out=Proto   --csharp_opt=file_extension=.g.cs

endlocal

