#!/usr/bin/env bash

if [[ !$1 ]]; then
    CONFIGURATION="Debug"
fi

if [[ $1 ]]; then
    CONFIGURATION=$1
fi

dotnet restore
dotnet build ./src/BetterReadLine -c $CONFIGURATION
dotnet build ./src/BetterReadLine.Demo -c $CONFIGURATION
dotnet build ./test/BetterReadLine.Tests -c $CONFIGURATION
