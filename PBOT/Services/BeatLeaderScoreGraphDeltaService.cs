using Newtonsoft.Json;
using PBOT.Models;
using SiraUtil.Logging;
using SiraUtil.Web;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PBOT.Services;

internal class BeatLeaderScoreGraphDeltaService : IDeltaService
{
    private readonly SiraLog _siraLog;
    private readonly IHttpService _httpService;
    private readonly IPlatformUserModel _platformUserModel;
    private const string _beatLeaderApiUrl = "https://api.beatleader.xyz";
    private CachedContractId? _cached;

    private record struct CachedContractId(int Id, ScoreContract Contract);
    private record struct ScoreGraphTracker([property: JsonProperty("graph")] float[] Graph);
    private record struct BeatLeaderScoreStatistics([property: JsonProperty("scoreGraphTracker")] ScoreGraphTracker Tracker);
    private record struct BeatLeaderScore([property: JsonProperty("id")] int Id, [property: JsonProperty("modifiedScore")] int TotalScore, [property: JsonProperty("timeset")] string TimeSet); // Why is the timestamp a string?
    private class BeatLeaderMetadata : DeltaMetadata { [JsonIgnore] public int Id { get; set; } }

    public BeatLeaderScoreGraphDeltaService(SiraLog siraLog, IHttpService httpService, IPlatformUserModel platformUserModel)
    {
        _siraLog = siraLog;
        _httpService = httpService;
        _platformUserModel = platformUserModel;
    }

    public async Task<IReadOnlyList<DeltaFrame>> GetFramesAsync(ScoreContract contract, CancellationToken cancellationToken = default)
    {
        _siraLog.Debug($"Fetching delta frames for {contract}");
        int? id = _cached?.Contract == contract ? _cached.Value.Id : null;
        if (_cached?.Contract != contract)
        {
            var metadata = await GetMetadataAsync(contract, cancellationToken);
            if (metadata is BeatLeaderMetadata beatLeaderMetadata)
                id = beatLeaderMetadata.Id;
        }

        if (id is null)
        {
            _siraLog.Debug($"Could not load delta frames for {contract}");
            return Array.Empty<DeltaFrame>();
        }

        _siraLog.Debug("Downloading statistics");
        var url = $"{_beatLeaderApiUrl}/score/statistic/{id.Value}";
        var response = await _httpService.GetAsync(url, cancellationToken: cancellationToken);
        if (!response.Successful)
        {
            _siraLog.Warn($"Could not download score statistic data for {contract}");
            return Array.Empty<DeltaFrame>();
        }


        _siraLog.Debug("Reading statistics response body");
        var data = await response.ReadAsStringAsync();
        var graph = JsonConvert.DeserializeObject<BeatLeaderScoreStatistics>(data).Tracker.Graph;

        List<DeltaFrame> frames = new(graph.Length + 1)
        {
            new DeltaFrame { Time = 0f, Current = 1f }
        };

        float second = 1f;
        foreach (var acc in graph)
            frames.Add(new DeltaFrame { Time = second++, Current = acc });

        return frames;
    }

    public async Task<DeltaMetadata?> GetMetadataAsync(ScoreContract contract, CancellationToken cancellationToken = default)
    {
        var (hash, mode, difficulty) = contract;

        _siraLog.Debug($"Loading metadata for {contract}");
        var user = await _platformUserModel.GetUserInfo();
        var url = $"{_beatLeaderApiUrl}/score/{user.platformUserId}/{hash}/{difficulty.SerializedName()}/{mode}";
        var response = await _httpService.GetAsync(url, cancellationToken: cancellationToken);
        if (!response.Successful)
        {
            _siraLog.Debug($"Could not find score for {contract}");
            return null;
        }

        _siraLog.Debug("Reading response body");
        var data = await response.ReadAsStringAsync();
        var (id, totalScore, timeSetString) = JsonConvert.DeserializeObject<BeatLeaderScore>(data);

        _siraLog.Debug("Caching replay url");
        _cached = new CachedContractId(id, contract);

        _siraLog.Debug("Generating metadata");
        return new BeatLeaderMetadata
        {
            Id = id,
            Source = "BeatLeader Score Graph",
            TotalScore = totalScore,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timeSetString)),
        };
    }

    public Task SaveAsync(ScoreContract score, DeltaMetadata metadata, List<DeltaFrame> frames, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}
