@echo off
cd GADownloader
dotnet publish -c Release -r win10-x64
cd ../GAProcessor
dotnet publish -c Release -r win10-x64
cd ..
warp-packer --arch windows-x64 --input_dir GADownloader/bin/Release/netcoreapp2.1/win10-x64/publish --exec GADownloader.exe --output GADownloader.exe
warp-packer --arch windows-x64 --input_dir GAProcessor/bin/Release/netcoreapp2.1/win10-x64/publish --exec GAProcessor.exe --output GAProcessor.exe