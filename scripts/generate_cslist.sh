#!/bin/bash

files=$(grep -o '<Compile Include="[^"]*"' PackageLicenseFilter.csproj | sed 's/<Compile Include="//; s/"//')
echo "$files" > PackageLicenseFilter.cslist
