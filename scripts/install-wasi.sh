#!/bin/sh

set -e

# Define the WASI version
export WASI_VERSION=20
export WASI_VERSION_FULL=${WASI_VERSION}.0
# Path to the hidden WASI SDK directory
install_path="$(readlink -f ~/.wasi-sdk)"
export WASI_SDK_PATH="$install_path/wasi-sdk-${WASI_VERSION_FULL}"

# Function to download and extract WASI SDK
download_and_extract() {
    os=$1
    file_name="wasi-sdk-${WASI_VERSION_FULL}-${os}.tar.gz"
    url="https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-${WASI_VERSION}/$file_name"
    # Download the file
    wget "$url"
    tar -xvf "$file_name" -C "$install_path"
    # Clean up: remove the downloaded tar.gz file
    rm "$file_name"
}

cleanup() {
    # Check if the tar file exists and delete it
    file_name="wasi-sdk-${WASI_VERSION_FULL}-$(uname -s).tar.gz"
    if [ -f "$file_name" ]; then
        rm "$file_name"
    fi
}

# Check if the .wasi-sdk directory exists and delete it if it does
if [ -d "$install_path" ]; then
    echo "Existing WASI SDK directory found. Deleting it..."
    rm -rf "$install_path"
fi

# Create a new hidden directory for WASI SDK
mkdir -p "$install_path"

# Check the operating system
case "$(uname -s)" in
Darwin)
    echo "MacOS detected"
    download_and_extract "macos"
    ;;
Linux)
    echo "Linux detected"
    download_and_extract "linux"
    ;;
*)
    echo "Unsupported operating system"
    exit 1
    ;;
esac

echo "WASI SDK installed successfully at $WASI_SDK_PATH"

# Check if wasi-experimental workload is already installed
if dotnet workload list | grep -q 'wasi-experimental'; then
    echo "WASI experimental workload is already installed."
else
    echo "Installing WASI experimental workload..."
    dotnet workload install wasi-experimental
    echo "WASI workload installed successfully"
fi
