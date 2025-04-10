#!/bin/bash

version=$(cat version.txt)

# for Client
cd Client

# Win:
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishTrimmed=true /p:TrimMode=link /p:PublishSingleFile=true /p:InvariantGlobalization=true /p:DebugType=None /p:DebugSymbols=false
# Lin:
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishTrimmed=true /p:TrimMode=link /p:PublishSingleFile=true /p:InvariantGlobalization=true /p:DebugType=None /p:DebugSymbols=false

cd ..
cd Server

# Lin:
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishTrimmed=true /p:TrimMode=link /p:PublishSingleFile=true /p:InvariantGlobalization=true /p:DebugType=None /p:DebugSymbols=false

cd ..

# Copy executables to the main directory
mv Client/bin/Release/net8.0/win-x64/publish/Client.exe ./ChatClient_win-x64-$version.exe
mv Client/bin/Release/net8.0/linux-x64/publish/Client ./ChatClient_linux-x64-$version
mv Server/bin/Release/net8.0/linux-x64/publish/Server ./ChatServer_linux-x64-$version

# Cleaning up the install
rm -rf Client/bin/*
rm -rf Client/obj/*
rm -rf Server/bin/*
rm -rf Server/obj/*