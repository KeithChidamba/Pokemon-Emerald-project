#!/bin/bash
cd "$HOME/Pokemon Luminary/Build"
rm index.html
# Copy index.html from template folder to /tmp
cp "/home/keith/Pokemon Luminary/Assets/WebGLTemplate/index.html" /tmp/index.html

# Move the copied index.html into the build directory
mv /tmp/index.html .



