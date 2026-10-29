# amcli, the AngouriMath terminal, as the release built it: `nix run github:asc-community/AngouriMath`.
# The version and the archives' hashes are in Sources/Terminal/AngouriMath.Terminal/packaging/release.json,
# which the Terminal release workflow updates after each release.
# https://github.com/asc-community/AngouriMath/issues/1660
{
  description = "amcli, the AngouriMath terminal and command-line calculator";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-25.05";

  outputs = { self, nixpkgs }:
    let
      release = builtins.fromJSON (builtins.readFile ./Sources/Terminal/AngouriMath.Terminal/packaging/release.json);
      runtimes = { x86_64-linux = "linux-x64"; aarch64-linux = "linux-arm64"; armv7l-linux = "linux-arm"; };
      forEachSystem = f: nixpkgs.lib.genAttrs (builtins.attrNames runtimes)
        (system: f nixpkgs.legacyPackages.${system} runtimes.${system});
    in
    {
      packages = forEachSystem (pkgs: runtime: rec {
        amcli = pkgs.stdenv.mkDerivation {
          pname = "amcli";
          inherit (release) version;
          src = pkgs.fetchurl {
            url = "https://github.com/asc-community/AngouriMath/releases/download/v${release.version}/angourimath-terminal-${runtime}.zip";
            sha256 = release.sha256.${runtime};
          };
          sourceRoot = runtime;
          nativeBuildInputs = [ pkgs.unzip pkgs.autoPatchelfHook pkgs.makeWrapper pkgs.installShellFiles ];
          buildInputs = [ pkgs.stdenv.cc.cc.lib pkgs.zlib pkgs.icu pkgs.openssl pkgs.krb5 ];
          # The runtime's tracing provider wants LTTng, which nothing here uses.
          postPatch = "rm -f libcoreclrtraceptprovider.so";
          dontBuild = true;
          # The assemblies are executable in the archive, and strip rewrites a PE file it is given:
          # a stripped System.Private.CoreLib.dll is a bad image to the runtime.
          dontStrip = true;
          installPhase = ''
            runHook preInstall
            mkdir -p $out/lib/amcli
            cp -r . $out/lib/amcli
            # .NET opens ICU and OpenSSL by name at run time, which autoPatchelf does not see.
            makeWrapper $out/lib/amcli/AngouriMath.Terminal $out/bin/amcli \
              --prefix LD_LIBRARY_PATH : ${pkgs.lib.makeLibraryPath [ pkgs.icu pkgs.openssl pkgs.zlib pkgs.krb5 ]}
            installManPage ${./Sources/Terminal/AngouriMath.Terminal/amcli.1}
            installShellCompletion --bash --name amcli ${./Sources/Terminal/AngouriMath.Terminal/amcli.bash}
            runHook postInstall
          '';
          meta = {
            description = "AngouriMath's terminal and command-line computer algebra system";
            homepage = "https://am.angouri.org";
            license = pkgs.lib.licenses.mit;
            sourceProvenance = [ pkgs.lib.sourceTypes.binaryNativeCode pkgs.lib.sourceTypes.binaryBytecode ];
            platforms = builtins.attrNames runtimes;
            mainProgram = "amcli";
          };
        };
        default = amcli;
      });
    };
}
