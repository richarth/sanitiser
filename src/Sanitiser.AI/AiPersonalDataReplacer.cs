using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.InlineChat;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Replacement;

namespace Umbraco.Community.Sanitiser;

public class AiPersonalDataReplacer(
    IAIChatService chatService,
    IOptions<AiReplacementOptions> options,
    ILogger<AiPersonalDataReplacer> logger) : IPersonalDataReplacer
{
    private readonly AiReplacementOptions _options = options.Value;

    // Generated people are buffered so a single AI call can cover many records, reducing cost and latency.
    // The replacer is a singleton and only ever called sequentially during startup sanitisation, so a plain
    // queue is safe.
    private readonly Queue<PersonalData> _buffer = new();

    public async Task<PersonalData> Replace(PersonalData original, int index, CancellationToken cancellationToken = default)
    {
        if (_buffer.Count == 0)
        {
            await FillBufferAsync(cancellationToken);
        }

        // Apply a deterministic per-record suffix so the email and username are always unique regardless of
        // what the model produced (users and members have unique email/username constraints).
        return _buffer.Count > 0
            ? MakeUnique(_buffer.Dequeue(), index)
            : Fallback(index);
    }

    private async Task FillBufferAsync(CancellationToken cancellationToken)
    {
        var batchSize = Math.Max(1, _options.BatchSize);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                $"You generate fictional personal data used to anonymise a database. Reply with ONLY a JSON " +
                $"array of {batchSize} objects, each with the keys \"name\", \"email\" and \"username\". Use " +
                "realistic full names, email addresses on the example.com domain, and usernames derived from " +
                "the names. Never use real people's details."),
            new(ChatRole.User, $"Generate {batchSize} distinct fictional people as a JSON array.")
        };

        // Bound the call so a slow or hung model cannot block startup, while still honouring host shutdown.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        try
        {
            ChatResponse response = await chatService.GetChatResponseAsync(Configure, messages, timeoutCts.Token);

            List<PersonalData> people = ParseArray(response.Text);
            if (people.Count == 0 && ParseObject(response.Text) is { } single)
            {
                people.Add(single);
            }

            foreach (PersonalData person in people)
            {
                _buffer.Enqueue(person);
            }

            if (_buffer.Count == 0)
            {
                logger.LogWarning("AI replacement response could not be parsed; falling back to templated values.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Host shutdown, not our timeout: propagate so the run stops.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI replacement request failed or timed out; falling back to templated values.");
        }
    }

    private void Configure(AIChatBuilder builder)
    {
        if (_options.ProfileAlias is { Length: > 0 } alias)
        {
            builder.WithProfile(alias);
        }
    }

    private static PersonalData MakeUnique(PersonalData person, int index)
    {
        var name = person.Name is { Length: > 0 } n ? n : $"user{index}";
        var username = person.Username is { Length: > 0 } u ? $"{u}_{index}" : $"user{index}";
        return new PersonalData(name, UniqueEmail(person.Email, index), username);
    }

    private static string UniqueEmail(string? email, int index)
    {
        var atIndex = email?.IndexOf('@') ?? -1;
        if (email is null || atIndex <= 0)
        {
            return $"user{index}@example.com";
        }

        // Insert the index into the local part, e.g. jane@example.com -> jane5@example.com.
        return $"{email[..atIndex]}{index}@{email[(atIndex + 1)..]}";
    }

    private static PersonalData Fallback(int index) => new($"user{index}", $"user{index}@example.com", $"user{index}");

    private static List<PersonalData> ParseArray(string? text)
    {
        var results = new List<PersonalData>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return results;
        }

        // Models sometimes wrap JSON in prose or code fences, so extract the first [...] block.
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            return results;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(text.Substring(start, end - start + 1));
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    if (ReadPerson(element) is { } person)
                    {
                        results.Add(person);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // ignored; the caller falls back
        }

        return results;
    }

    private static PersonalData? ParseObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(text.Substring(start, end - start + 1));
            return ReadPerson(doc.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static PersonalData? ReadPerson(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var name = element.TryGetProperty("name", out var n) ? n.GetString() : null;
        var email = element.TryGetProperty("email", out var e) ? e.GetString() : null;
        var username = element.TryGetProperty("username", out var u) ? u.GetString() : null;

        if (name is null && email is null && username is null)
        {
            return null;
        }

        return new PersonalData(name, email, username);
    }
}
