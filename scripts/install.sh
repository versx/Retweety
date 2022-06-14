# Download .NET 5.0 installer
curl https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh > dotnet-install.sh
echo "Downloading .NET 5.0 installer..."

# Make installer executable
echo "Setting executable permissions..."
chmod +x dotnet-install.sh

# Install .NET 5.0 SDK
echo "Launching .NET installer..."
./dotnet-install.sh --version 5.0.404

# Delete .NET Core 5.0 installer
echo "Deleting .NET installer..."
rm dotnet-install.sh

# Clone repository
echo "Cloning repository..."
git clone https://github.com/versx/Retweety

# Change directory into cloned repository
echo "Changing directory..."
cd Retweety

# Build Retweety.dll
echo "Building Retweety..."
~/.dotnet/dotnet build

# Copy example config
echo "Copying example config..."
cp config.example.json bin/config.json

echo "Changing directory to build folder..."
cd bin