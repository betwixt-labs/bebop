#!/bin/sh

# the version the nuget package will be built with
VERSION=$1

NUGET_PATH="$PWD/.nuget"

install_mono()
{

    REQUIRED_PKG="mono-complete"
    PKG_OK=$(dpkg-query -W --showformat='${Status}\n' $REQUIRED_PKG|grep "install ok installed")
    echo Checking for $REQUIRED_PKG: $PKG_OK
    if [ "" = "$PKG_OK" ]; then
        echo "No $REQUIRED_PKG. Setting up $REQUIRED_PKG."
        sudo apt -y install gnupg ca-certificates
        sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
        echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
        sudo apt -y update
        sudo apt -y install mono-complete
    fi
}

download_nuget() 
{
    if [ -f "$NUGET_PATH/nuget.exe" ]; then
        echo "$NUGET_PATH exists."
    else    
        echo "$NUGET_PATH does not exist. Downloading."
        wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -P $NUGET_PATH
    fi
}

echo "Building bebop-tools for .NET"
install_mono
download_nuget
mono $NUGET_PATH/nuget.exe Pack bebop-tools.nuspec -OutputDirectory packages -Version $VERSION