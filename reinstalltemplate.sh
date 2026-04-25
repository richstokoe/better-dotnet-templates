#!/bin/sh

### Removes existing templated (if any)

dotnet new uninstall RichStokoe.BetterTemplates

### Builds and re-packs the nuget

PACKAGE_VERSION=$(grep '<PackageVersion>' BetterTemplates.csproj | tr -d ' ' | sed 's/<PackageVersion>//;s/<\/PackageVersion>//')
rm -f *.sln
rm -f "bin/Release/RichStokoe.BetterTemplates.${PACKAGE_VERSION}.nupkg"
dotnet pack -c Release

dotnet new install ${PWD}/bin/Release/RichStokoe.BetterTemplates.${PACKAGE_VERSION}.nupkg
