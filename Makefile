all: main linux_publish copy_bins_to_toplevel linux_test

main:
	@echo "Building main... ---"
	dotnet build xfspy

#linux_build:
#	@echo "Building linux_build... ---"
#	dotnet build likeNotepad.Gtk -p:PublishSingleFile=true --self-contained true

linux_publish:
	@echo "Building linux_publish... ---"
	dotnet publish xfspy.Gtk -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true

copy_bins_to_toplevel:
	mv xfspy.Gtk/bin/Release/net8.0/linux-x64/publish/xfspy.Gtk ./xfspy_linux64

linux_test:
	./xfspy_linux64