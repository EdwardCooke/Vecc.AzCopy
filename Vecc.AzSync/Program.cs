using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Vecc.AzSync
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .Add(new EnvFileConfigurationSource())
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            var configuration = configurationBuilder.Build();
            var sourcePath = configuration["sourcePath"];
            var targetContainer = configuration["targetContainer"];
            var sasToken = configuration["sasToken"];
            var targetPath = configuration["targetPath"] ?? string.Empty;
            var dryRun = configuration["dryRun"] == "true";
            Console.CursorVisible = false;

            if (string.IsNullOrWhiteSpace(sourcePath) ||
                string.IsNullOrWhiteSpace(targetContainer) ||
                string.IsNullOrWhiteSpace(sasToken))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine(" --sourcePath        The source of files to sync, these will be placed in the target container url. For example, /data");
                Console.WriteLine(" --targetContainer   That URL to container where to place the files.");
                Console.WriteLine("                     Example: https://storageaccount.blob.core.windows.net/container");
                Console.WriteLine("- -sasToken          The SAS token to use to connect to the blob storage container.");
                Console.WriteLine("                     The following options need to be enabled for this to be succesful.");
                Console.WriteLine("                     Read, Add, Create, Write, Delete, List");
                Console.WriteLine(" --targetPath        Optional. Defines the path inside of the container to store the files.");
                Console.WriteLine("                     If omitted it will store them in the root of the container.");
                Console.WriteLine(" --dryRun            Optional. If set to true then no operations will be performed on the containter.");
                Console.WriteLine("                     It will only show what is going to be done.");

                Environment.Exit(-1);
            }

            var containerClient = new BlobContainerClient(new Uri(targetContainer),
                new Azure.AzureSasCredential(sasToken));

            Console.WriteLine("Getting current source files and target blobs");

            var blobTask = GetCurrentBlobsAsync(containerClient);
            var fileTask = GetCurrentFileSystemFilesAsync(sourcePath, targetPath);

            await Task.WhenAll(blobTask, fileTask);

            Console.WriteLine("Done getting source files and target blobs");

            var fileDictionary = fileTask.Result.ToDictionary(x => x.Name);
            var blobDictionary = blobTask.Result.ToDictionary(x => x.Name);

            Console.WriteLine("Source file count: {0}", fileDictionary.Count);
            Console.WriteLine("Target blob count: {0}", blobDictionary.Count);

            var filesToUpload = fileTask.Result.Where(file => ShouldUpload(file, blobDictionary)).ToArray();
            var filesToDelete = blobTask.Result.Where(blob => !fileDictionary.ContainsKey(blob.Name)).ToArray();

            Console.WriteLine("Upload count: {0}", filesToUpload.Length);
            Console.WriteLine("Upload byte count: {0}", filesToUpload.Sum(x => x.Size).ToString("###,###,###,###,###"));
            Console.WriteLine("Delete count: {0}", filesToDelete.Length);
            Console.WriteLine("=======================================");

            if (filesToUpload.Length > 0)
            {
                Console.WriteLine("Uploading...");
                foreach (var file in filesToUpload)
                {
                    UploadBlob(containerClient, file, dryRun);
                }
            }

            if (filesToDelete.Length > 0)
            {
                Console.WriteLine("Downloading...");
                foreach (var blob in filesToDelete)
                {
                    DeleteBlob(containerClient, blob.Name, dryRun);
                }
            }

            if (filesToUpload.Length == 0 && filesToDelete.Length == 0)
            {
                Console.WriteLine("Nothing to do, destination is already in sync.");
            }

            Console.WriteLine("=======================================");
            Console.WriteLine("Complete");
        }

        private static void DeleteBlob(BlobContainerClient containerClient, string path, bool dryRun)
        {
            Console.WriteLine("Deleting: {0}", path);

            if (!dryRun)
            {
                try
                {
                    var blobClient = containerClient.GetBlobClient(path);
                    blobClient.DeleteIfExists(DeleteSnapshotsOption.IncludeSnapshots);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error while deleting blob: {0}", exception);
                }
            }
        }

        private static Task<List<FileMetaData>> GetCurrentBlobsAsync(BlobContainerClient client) => Task.Run(() =>
        {
            var pagedBlobs = client.GetBlobs(BlobTraits.None, BlobStates.None);
            var pageIndex = 1;
            var blobIndex = 1;
            var blobs = new List<FileMetaData>();

            foreach (var page in pagedBlobs.AsPages())
            {
                Console.WriteLine("Processing page: {0}", pageIndex);
                foreach (var blob in page.Values)
                {
                    if (blob.Deleted)
                    {
                        continue;
                    }

                    var smallBlob = new FileMetaData
                    {
                        Date = blob.Properties.LastModified ?? blob.Properties.CreatedOn ?? DateTimeOffset.UtcNow,
                        Name = blob.Name,
                        Size = blob.Properties.ContentLength ?? 0
                    };

                    blobs.Add(smallBlob);

                    blobIndex++;
                }
                pageIndex++;
            }
            return Task.FromResult(blobs);
        });

        private static Task<List<FileMetaData>> GetCurrentFileSystemFilesAsync(string folderPath, string rootPath) => Task.Run(async () =>
        {
            var result = new List<FileMetaData>();
            try
            {
                var directoryTasks = new List<Task<List<FileMetaData>>>();

                foreach (var directory in Directory.GetDirectories(folderPath))
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    string newPath;
                    if (!string.IsNullOrWhiteSpace(rootPath))
                    {
                        newPath = $"{rootPath}/{directoryInfo.Name}";
                    }
                    else
                    {
                        newPath = directoryInfo.Name;
                    }

                    directoryTasks.Add(GetCurrentFileSystemFilesAsync(directory, newPath));
                }

                foreach (var file in Directory.GetFiles(folderPath))
                {
                    var fileInfo = new FileInfo(file);
                    var smallBlob = new FileMetaData
                    {
                        Date = fileInfo.LastWriteTimeUtc,
                        FullFilePath = file,
                        Name = $"{rootPath}/{fileInfo.Name}",
                        Size = fileInfo.Length
                    };
                    result.Add(smallBlob);
                }

                result.AddRange((await Task.WhenAll(directoryTasks)).SelectMany(x => x));
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine("Error:" + exception);
                Console.WriteLine("Error:" + exception);
            }
            return result;
        });

        private static bool ShouldUpload(FileMetaData source, IDictionary<string, FileMetaData> blobs)
        {
            if (!blobs.TryGetValue(source.Name, out var target))
            {
                return true;
            }

            if (source.Date > target.Date)
            {
                return true;
            }

            if (source.Size != target.Size)
            {
                return true;
            }

            return false;
        }

        private static void UploadBlob(BlobContainerClient containerClient, FileMetaData file, bool dryRun)
        {
            Console.WriteLine("Uploading: {0} -> {1}", file.FullFilePath, file.Name);

            if (!dryRun)
            {
                try
                {
                    var blobClient = containerClient.GetBlobClient(file.Name);
                    using var stream = new StreamReader(file.FullFilePath);
                    blobClient.Upload(stream.BaseStream, new BlobUploadOptions { ProgressHandler = new ProgressMonitor(file) });
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error while uploading blob: {0}", exception);
                }
            }
        }
    }
}
