#!/bin/bash

# Unity Package Export Script
# Creates a .unitypackage file for easy distribution

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_NAME="FastVLM-Unity-Plugin"
VERSION="1.0.0"
OUTPUT_DIR="$SCRIPT_DIR/Packages"
TEMP_DIR="/tmp/fastvlm-unity-export"

echo "📦 FastVLM Unity Package Exporter"
echo "================================="

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Clean temp directory
if [ -d "$TEMP_DIR" ]; then
    rm -rf "$TEMP_DIR"
fi
mkdir -p "$TEMP_DIR"

echo "🔨 Preparing package files..."

# Copy Unity package files
cp -r "$SCRIPT_DIR/Runtime" "$TEMP_DIR/"
cp -r "$SCRIPT_DIR/Editor" "$TEMP_DIR/"
cp -r "$SCRIPT_DIR/Samples~" "$TEMP_DIR/"

# Copy package files
cp "$SCRIPT_DIR/package.json" "$TEMP_DIR/"
cp "$SCRIPT_DIR/README.md" "$TEMP_DIR/"
cp "$SCRIPT_DIR/CHANGELOG.md" "$TEMP_DIR/"
cp "$SCRIPT_DIR/LICENSE" "$TEMP_DIR/"
cp "$SCRIPT_DIR/QUICKSTART.md" "$TEMP_DIR/"

# Create package info
cat > "$TEMP_DIR/PACKAGE_INFO.md" << EOF
# FastVLM Unity Plugin v$VERSION

## Package Contents

- **Runtime/**: Core plugin scripts and native libraries
- **Editor/**: Build processors and Unity editor tools
- **Samples~/**: Example scenes and scripts
- **Documentation**: README, setup guides, and examples

## Installation

1. Import this .unitypackage into your Unity project
2. Run the setup script: \`./setup_ios.sh\`
3. Configure iOS build settings
4. Build and deploy to iOS device

## Requirements

- Unity 2021.3+
- iOS 15.0+
- Xcode 14.0+
- macOS development environment

For complete documentation, see README.md
EOF

# Create a tarball (Unity will need to be used to create actual .unitypackage)
cd "$TEMP_DIR"
tar -czf "$OUTPUT_DIR/${PACKAGE_NAME}-${VERSION}.tar.gz" .
cd "$SCRIPT_DIR"

echo "✅ Package created: $OUTPUT_DIR/${PACKAGE_NAME}-${VERSION}.tar.gz"
echo ""
echo "📋 To create a .unitypackage file:"
echo "1. Import the extracted files into a Unity project"
echo "2. Select all FastVLM assets in Project window"
echo "3. Right-click and choose 'Export Package...'"
echo "4. Export as ${PACKAGE_NAME}-${VERSION}.unitypackage"
echo ""
echo "🔗 Package contents available at: $TEMP_DIR"

# Create installation script
cat > "$OUTPUT_DIR/install.sh" << 'EOF'
#!/bin/bash
echo "🚀 FastVLM Unity Plugin Installation"
echo "Please follow these steps:"
echo ""
echo "1. Extract the package files to your Unity project's Assets folder"
echo "2. Run the setup script from your project root:"
echo "   chmod +x Assets/FastVLM/setup_ios.sh"
echo "   ./Assets/FastVLM/setup_ios.sh"
echo "3. Follow the QuickStart guide for your first implementation"
echo ""
echo "📚 Documentation: README.md"
echo "🚀 Quick Start: QUICKSTART.md"
echo "🔧 Setup Guide: SETUP_GUIDE.md"
EOF

chmod +x "$OUTPUT_DIR/install.sh"

echo "📝 Installation script created: $OUTPUT_DIR/install.sh"
echo ""
echo "🎉 Package export complete!"
echo "Distribution files:"
echo "  • ${PACKAGE_NAME}-${VERSION}.tar.gz"
echo "  • install.sh"
echo ""
