{ pkgs, ... }:
{
  # Which nixpkgs channel to use.
  channel = "stable-23.11"; # Or "unstable".

  # Use https://search.nixos.org/packages to find packages.
  packages = [
    pkgs.dotnet-sdk_8
  ];

  # Sets environment variables in the workspace.
  env = {};

  # Search for the extensions you want on https://open-vsx.org/ and use "publisher.id" here.
  extensions = [];

  # Enable previews and customize configuration parameters for the extensions you selected above.
  previews = {};
}