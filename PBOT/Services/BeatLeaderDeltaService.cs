using BeatLeader.Models;
using Newtonsoft.Json;
using PBOT.Models;
using SiraUtil.Logging;
using SiraUtil.Web;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static ScoreModel;

namespace PBOT.Services;

internal class BeatLeaderDeltaService : IDeltaService
{
    private readonly SiraLog _siraLog;
    private readonly IHttpService _httpService;
    private readonly IPlatformUserModel _platformUserModel;
    private const string _beatLeaderApiUrl = "https://api.beatleader.xyz";
    private CachedContractReplay? _cached;

    private record struct CachedContractReplay(ScoreContract Contract, string ReplayUrl);
    private record struct BeatLeaderScore([property: JsonProperty("replay")] string Replay, [property: JsonProperty("timeset")] string TimeSet, [property: JsonProperty("modifiedScore")] int TotalScore); // Why is the timestamp a string?
    private class BeatLeaderMetadata : DeltaMetadata { [JsonIgnore] public string ReplayUrl { get; set; } = null!; }

    public BeatLeaderDeltaService(SiraLog siraLog, IHttpService httpService, IPlatformUserModel platformUserModel)
    {
        _siraLog = siraLog;
        _httpService = httpService;
        _platformUserModel = platformUserModel;
    }

    public async Task<IReadOnlyList<DeltaFrame>> GetFramesAsync(ScoreContract contract, CancellationToken cancellationToken = default)
    {
        _siraLog.Debug($"Fetching delta frames for {contract}");
        string? replayUrl = _cached?.Contract == contract ? _cached.Value.ReplayUrl : null;
        if (_cached?.Contract != contract)
        {
            var metadata = await GetMetadataAsync(contract, cancellationToken);
            if (metadata is BeatLeaderMetadata beatLeaderMetadata)
                replayUrl = beatLeaderMetadata.ReplayUrl;
        }

        if (replayUrl is null)
        {
            _siraLog.Debug($"Could not load delta frames for {contract}");
            return Array.Empty<DeltaFrame>();
        }

        _siraLog.Debug("Downloading replay");
        var response = await _httpService.GetAsync(replayUrl, cancellationToken: cancellationToken);
        if (!response.Successful)
        {
            _siraLog.Warn($"Could not download replay data for {contract}");
            return Array.Empty<DeltaFrame>();
        }

        var bytes = await response.ReadAsByteArrayAsync();
        var replayDecoderMethod = typeof(Replay).Assembly.GetType("BeatLeader.Models.ReplayDecoder").GetMethod("Decode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var replay = (replayDecoderMethod.Invoke(null, new object[] { bytes }) as Replay)!;

        List<ScoreEvent> events = new();
        foreach (var note in replay.notes)
            events.Add(new ScoreEvent(note.eventTime, note));
        foreach (var wall in replay.walls)
            events.Add(new ScoreEvent(wall.time, wall));

        events.Sort((scoreEvent, otherEvent) => scoreEvent.Time.CompareTo(otherEvent.Time));

        ScoreMultiplierCounter counter = new();
        List<DeltaFrame> frames = new();

        int maxScore = 0;
        int currentScore = 0;
        foreach (var scoreEvent in events)
        {
            var isWall = scoreEvent.Wall is not null;
            var noteType = scoreEvent.Note!.eventType;
            var multiplier = isWall || noteType is NoteEventType.bad || noteType is NoteEventType.bomb || noteType is NoteEventType.miss ? ScoreMultiplierCounter.MultiplierEventType.Negative : ScoreMultiplierCounter.MultiplierEventType.Positive;

            counter.ProcessMultiplierEvent(multiplier);
            if (isWall || noteType is not NoteEventType.good)
                continue;

            var cutInfo = scoreEvent.Note!.noteCutInfo;
            NoteParams noteParams = new(scoreEvent.Note!.noteID);
            var definition = GetDefinition(noteParams.scoringType);
            var centerCutScore = Mathf.RoundToInt(definition.maxCenterDistanceCutScore * (1f - Mathf.Clamp01(cutInfo.cutDistanceToCenter / 0.3f)));
            var beforeCutScore = Mathf.RoundToInt(Mathf.LerpUnclamped(definition.minBeforeCutScore, definition.maxBeforeCutScore, cutInfo.beforeCutRating));
            var afterCutScore = Mathf.RoundToInt(Mathf.LerpUnclamped(definition.minAfterCutScore, definition.maxAfterCutScore, cutInfo.afterCutRating));
            var noteScore = afterCutScore + beforeCutScore + centerCutScore + definition.fixedCutScore;

            currentScore += counter.multiplier * noteScore;
            maxScore += counter.multiplier * definition.maxCutScore;

            var accuracy = currentScore / (float)maxScore;
            frames.Add(new DeltaFrame { Time = scoreEvent.Time, Current = accuracy });
        }

        return frames;
    }

    private static NoteScoreDefinition GetDefinition(ScoringType scoringType) => scoringType switch
    {
        ScoringType.Default => GetNoteScoreDefinition(NoteData.ScoringType.NoScore),
        ScoringType.Ignore => GetNoteScoreDefinition(NoteData.ScoringType.Ignore),
        ScoringType.NoScore => GetNoteScoreDefinition(NoteData.ScoringType.NoScore),
        ScoringType.Normal => GetNoteScoreDefinition(NoteData.ScoringType.Normal),
        ScoringType.SliderHead => GetNoteScoreDefinition(NoteData.ScoringType.SliderHead),
        ScoringType.SliderTail => GetNoteScoreDefinition(NoteData.ScoringType.SliderTail),
        ScoringType.BurstSliderHead => GetNoteScoreDefinition(NoteData.ScoringType.BurstSliderHead),
        ScoringType.BurstSliderElement => GetNoteScoreDefinition(NoteData.ScoringType.BurstSliderElement),
        _ => GetNoteScoreDefinition(NoteData.ScoringType.Ignore)
    };

    public async Task<DeltaMetadata?> GetMetadataAsync(ScoreContract contract, CancellationToken cancellationToken = default)
    {
        var (hash, mode, difficulty) = contract;

        _siraLog.Debug($"Loading metadata for {contract}");
        var user = await _platformUserModel.GetUserInfo();
        var url = $"{_beatLeaderApiUrl}/score/{3225556157461414}/{hash}/{difficulty.SerializedName()}/{mode}";
        var response = await _httpService.GetAsync(url, cancellationToken: cancellationToken);
        if (!response.Successful)
        {
            _siraLog.Debug($"Could not find score for {contract}");
            return null;
        }

        _siraLog.Debug("Reading response body");
        var data = await response.ReadAsStringAsync();
        var (replayUrl, timeSetString, totalScore) = JsonConvert.DeserializeObject<BeatLeaderScore>(data);

        _siraLog.Debug("Caching replay url");
        _cached = new CachedContractReplay(contract, replayUrl);

        _siraLog.Debug("Generating metadata");
        return new BeatLeaderMetadata
        {
            Source = "BeatLeader",
            ReplayUrl = replayUrl,
            TotalScore = totalScore,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(timeSetString)),
        };
    }

    public Task SaveAsync(ScoreContract score, DeltaMetadata metadata, List<DeltaFrame> frames, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    class ScoreEvent
    {
        public float Time { get; }

        public NoteEvent? Note { get; }

        public WallEvent? Wall { get; }

        public ScoreEvent(float time, NoteEvent note)
        {
            Time = time;
            Note = note;
        }

        public ScoreEvent(float time, WallEvent wall)
        {
            Time = time;
            Wall = wall;
        }
    }

    enum ScoringType
    {
        Default,
        Ignore,
        NoScore,
        Normal,
        SliderHead,
        SliderTail,
        BurstSliderHead,
        BurstSliderElement
    }

    class NoteParams
    {
        public ScoringType scoringType;
        public int lineIndex;
        public int noteLineLayer;
        public int colorType;
        public int cutDirection;

        public NoteParams(int noteId)
        {
            int id = noteId;
            if (id < 100000)
            {
                scoringType = (ScoringType)(id / 10000);
                id -= (int)scoringType * 10000;

                lineIndex = id / 1000;
                id -= lineIndex * 1000;

                noteLineLayer = id / 100;
                id -= noteLineLayer * 100;

                colorType = id / 10;
                cutDirection = id - colorType * 10;
            }
            else
            {
                scoringType = (ScoringType)(id / 10000000);
                id -= (int)scoringType * 10000000;

                lineIndex = id / 1000000;
                id -= lineIndex * 1000000;

                noteLineLayer = id / 100000;
                id -= noteLineLayer * 100000;

                colorType = id / 10;
                cutDirection = id - colorType * 10;
            }
        }
    }
}