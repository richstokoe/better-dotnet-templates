#!/bin/sh

### Removes existing templated (if any)

dotnet new uninstall RichStokoe.BetterTemplates

### Builds and re-packs the nuget 

rm *.sln
dotnet pack -c Release

dotnet new install ${PWD}/bin/Release/RichStokoe.BetterTemplates.2.0.0.nupkg
