#!/bin/bash

files=$(grep -o '<Compile Include="[^"]*"' VarLicenseFilter.csproj | sed 's/<Compile Include="//; s/"//')
echo "$files" > VarLicenseFilter.cslist
