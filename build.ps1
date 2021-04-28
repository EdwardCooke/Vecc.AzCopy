dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r linux-arm -p:PublishSingleFile=true
dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r linux-arm64 -p:PublishSingleFile=true
dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r linux-x64 -p:PublishSingleFile=true
dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r win-x64 -p:PublishSingleFile=true


docker build -t multiarch-example:manifest-amd64 --build-arg ARCH=amd64/ --build-arg IMGPATH=linux-x64 -f Dockerfile.build .
docker build -t multiarch-example:manifest-arm32v7 --build-arg ARCH=arm32v7/ --build-arg IMGPATH=linux-arm -f Dockerfile.build .
docker build -t multiarch-example:manifest-arm64v8 --build-arg ARCH=arm64v8/ --build-arg IMGPATH=linux-arm64 -f Dockerfile.build .

docker manifest create `
    multiarch-example:latest `
    --amend multiarch-example:manifest-amd64 `
    --amend multiarch-example:manifest-arm32v7 `
    --amend multiarch-example:manifest-arm64v8