/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text.Json;
using System.Threading.Channels;
using Match.Get5.Events;

namespace Match;

public class MatchEventStore
{
    private readonly string _inProgressPath;
    private readonly string _completedPath;
    private readonly Channel<JsonElement> _channel;
    private readonly List<JsonElement> _events = [];
    private readonly Task _writerTask;

    public MatchEventStore(string matchPath)
    {
        _inProgressPath = Path.Combine(matchPath, "in_progress.json");
        _completedPath = Path.Combine(matchPath, "completed.json");
        _channel = Channel.CreateUnbounded<JsonElement>(
            new UnboundedChannelOptions { SingleReader = true }
        );
        _writerTask = Task.Run(ProcessAsync);
    }

    private async Task ProcessAsync()
    {
        await foreach (var element in _channel.Reader.ReadAllAsync())
        {
            _events.Add(element);
            try
            {
                File.WriteAllText(_inProgressPath, JsonSerializer.Serialize(_events));
            }
            catch (Exception ex)
            {
                Swiftly.Log($"Error writing in_progress.json: {ex.Message}");
            }
        }
    }

    public void Enqueue(Get5Event data) =>
        _channel.Writer.TryWrite(JsonSerializer.SerializeToElement(data, data.GetType()));

    public void Complete()
    {
        _channel.Writer.Complete();
        _writerTask.Wait();
        IoHelper.WriteJson(_completedPath, _events);
        if (File.Exists(_inProgressPath))
            File.Delete(_inProgressPath);
    }

    public void Cancel()
    {
        _channel.Writer.TryComplete();
        _writerTask.Wait();
    }
}
