#!/bin/bash
cd "/home/keith/Pokemon Proj Build/"
rm index.html
# Copy index.html from template folder to /tmp
cp "$HOME/Pokemon Luminary/Assets/WebGL Template/index.html" /tmp/index.html

# Move the copied index.html into the build directory
mv /tmp/index.html .



