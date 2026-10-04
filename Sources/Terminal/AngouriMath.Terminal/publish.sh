#!/bin/bash


# The release's version, which the workflow passes as TERMINAL_VERSION, goes into the assemblies
# as well as the package names: FileVersion is four numbers, the version without its suffix and
# a zero. https://github.com/asc-community/AngouriMath/issues/1678
version_properties=()
if [ -n "${TERMINAL_VERSION:-}" ]; then
    version_properties=(-p:Version="$TERMINAL_VERSION" -p:FileVersion="${TERMINAL_VERSION%%[-+]*}.0")
fi

# Portable runtime identifiers: since .NET 8 a versioned one such as osx.12-arm64 is not
# recognised, and the publish fails with NETSDK1083.
for arch in linux-x64 linux-arm linux-arm64 win-x64 win-x86 osx-x64 osx-arm64
do
    dotnet publish \
    -r $arch \
    -c Release \
    -o ./publish-output/$arch \
    --self-contained \
    "${version_properties[@]}"
done

