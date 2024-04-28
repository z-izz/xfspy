#!/bin/bash

# make sure we are sudo
sudo echo "Building deb..."

echo "Creating directories..."
# make directories
mkdir xfspy-deb
mkdir xfspy-deb/DEBIAN
mkdir xfspy-deb/usr
mkdir xfspy-deb/usr/bin
mkdir xfspy-deb/usr/share
mkdir xfspy-deb/usr/share/applications

echo "Creating DEBIAN/control file..."
cat <<EOF > xfspy-deb/DEBIAN/control
Package: xfspy
Version: 1.0.0
Section: utils
Priority: optional
Architecture: all
Depends: xfconf
Maintainer: z-izz <dev@gordae.com>
Description: Monitors changes to XFCE settings, shows the command done, and adds option to revert settings. 
Homepage: https://github.com/z-izz/xfspy/
EOF

echo "Adding the .desktop file..."
cat <<EOF > xfspy-deb/usr/share/applications/xfspy.desktop
[Desktop Entry]
Name=xfspy
Comment=XFCE settings monitor
Keywords=settings,spy,monitor
Exec=xfspy
Terminal=false
Type=applications
Categories=X-XFCE;Settings;DesktopSettings;Development
OnlyShowIn=XFCE;
EOF

echo "Adding the program..."
# add the program file
cp xfspy_linux64 xfspy-deb/usr/bin/xfspy

echo "Changing permissions for the deb..."
# give root permissions to the dir
sudo chown -R root:root xfspy-deb
sudo chmod 0755 xfspy-deb/DEBIAN
sudo chmod 0755 xfspy-deb/usr/bin/*
sudo chmod 0755 xfspy-deb/usr/share/applications/*

echo "Packing deb..."
# build the deb
sudo dpkg-deb --build xfspy-deb