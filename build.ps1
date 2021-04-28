dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r linux-arm -p:PublishSingleFile=true
dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r linux-arm64 -p:PublishSingleFile=true
dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r linux-x64 -p:PublishSingleFile=true
dotnet publish Vecc.AzSync/Vecc.AzSync.csproj -c Release -r win-x64 -p:PublishSingleFile=true


docker build -t crvecc.azurecr.io/test/multiarch-example:manifest-amd64 --build-arg ARCH=amd64 --build-arg IMGPATH=linux-x64 -f Dockerfile.build .
docker build -t crvecc.azurecr.io/test/multiarch-example:manifest-arm32v7 --build-arg ARCH=arm32v7 --build-arg IMGPATH=linux-arm -f Dockerfile.build .
docker build -t crvecc.azurecr.io/test/multiarch-example:manifest-arm64v8 --build-arg ARCH=arm64v8 --build-arg IMGPATH=linux-arm64 -f Dockerfile.build .

docker image push crvecc.azurecr.io/test/multiarch-example:manifest-amd64
docker image push crvecc.azurecr.io/test/multiarch-example:manifest-arm32v7
docker image push crvecc.azurecr.io/test/multiarch-example:manifest-arm64v8

docker manifest create `
    crvecc.azurecr.io/test/multiarch-example:latest1 `
    --amend crvecc.azurecr.io/test/multiarch-example:manifest-amd64 `
    --amend crvecc.azurecr.io/test/multiarch-example:manifest-arm32v7 `
    --amend crvecc.azurecr.io/test/multiarch-example:manifest-arm64v8

docker manifest push crvecc.azurecr.io/test/multiarch-example:latest1