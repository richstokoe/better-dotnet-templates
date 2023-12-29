#!/bin/sh

### Removes the template

dotnet new uninstall RichStokoe.BetterTemplates

## Adds the template

dotnet pack -c Release

dotnet new install ${PWD}/bin/Release/RichStokoe.BetterTemplates.1.0.0.nupkg
