using IPA.Utilities;
using Newtonsoft.Json;
using PBOT.Models;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PBOT.Services;

internal class FileSystemDeltaService : IDeltaService
{
    private static readonly DirectoryInfo _storageDirectory = new(Path.Combine(UnityGame.UserDataPath, "PBOT", "Storage"));

    public Task<IReadOnlyList<DeltaFrame>> GetFramesAsync(ScoreContract contract, CancellationToken cancellationToken = default)
    {
        CreateDirectory();
        var file = Path.Combine(_storageDirectory.FullName, $"{contract}.deltaf");
        if (!File.Exists(file))
            return Task.FromResult<IReadOnlyList<DeltaFrame>>(Array.Empty<DeltaFrame>());

        using var frameFileStream = File.OpenRead(file);
        var frames = Serializer.Deserialize<List<DeltaFrame>>(frameFileStream);
        return Task.FromResult<IReadOnlyList<DeltaFrame>>(frames);
    }

    public Task<DeltaMetadata?> GetMetadataAsync(ScoreContract contract, CancellationToken cancellationToken = default)
    {
        CreateDirectory();
        var file = Path.Combine(_storageDirectory.FullName, $"{contract}.delta");
        if (!File.Exists(file))
            return Task.FromResult<DeltaMetadata?>(null);

        var metadataString = File.ReadAllText(file);
        var metadata = JsonConvert.DeserializeObject<DeltaMetadata?>(metadataString);
        return Task.FromResult(metadata);
    }

    public Task SaveAsync(ScoreContract score, DeltaMetadata metadata, List<DeltaFrame> frames, CancellationToken cancellationToken = default)
    {
        CreateDirectory();
        File.WriteAllText(Path.Combine(_storageDirectory.FullName, $"{score}.delta"), JsonConvert.SerializeObject(metadata));
        using var frameFileStream = File.Create(Path.Combine(_storageDirectory.FullName, $"{score}.deltaf"));
        Serializer.Serialize(frameFileStream, frames);
        return Task.CompletedTask;
    }

    private static void CreateDirectory()
    {
        if (_storageDirectory.Exists)
            return;

        _storageDirectory.Create();
    }
}