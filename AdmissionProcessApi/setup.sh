#!/bin/bash

# Setup script for Masterschool Exercise
# This script helps install .NET SDK if not already installed

echo "🔍 Checking for .NET SDK..."

if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET SDK is already installed: $DOTNET_VERSION"
    
    # Check if it's .NET 8 or later
    MAJOR_VERSION=$(echo $DOTNET_VERSION | cut -d. -f1)
    if [ "$MAJOR_VERSION" -ge 8 ]; then
        echo "✅ Version is compatible (8.0 or later)"
        echo ""
        echo "🚀 You're ready to go! Run:"
        echo "   dotnet restore"
        echo "   dotnet run"
        exit 0
    else
        echo "⚠️  Version $DOTNET_VERSION is too old. Please install .NET 8 or later."
    fi
else
    echo "❌ .NET SDK is not installed."
    echo ""
    echo "📦 To install .NET SDK:"
    echo ""
    echo "Option 1: Using Homebrew (macOS)"
    echo "   brew install dotnet"
    echo ""
    echo "Option 2: Download from Microsoft"
    echo "   Visit: https://dotnet.microsoft.com/download/dotnet/8.0"
    echo "   Download and install the .NET 8 SDK for macOS"
    echo ""
    echo "After installation, run this script again or:"
    echo "   dotnet restore"
    echo "   dotnet run"
    exit 1
fi
