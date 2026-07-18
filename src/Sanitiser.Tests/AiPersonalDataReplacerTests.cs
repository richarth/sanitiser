using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.InlineChat;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Replacement;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests;

public class AiPersonalDataReplacerTests
{
    [Fact]
    public async Task Replace_parses_a_json_response_and_makes_it_unique_per_record()
    {
        var replacer = ReplacerFor("{\"name\":\"Jane Doe\",\"email\":\"jane@example.com\",\"username\":\"jane\"}");

        PersonalData result = await replacer.Replace(new PersonalData("x", "x", "x"), 3);

        Assert.Equal("Jane Doe", result.Name);
        // the record index is woven into the email and username to guarantee uniqueness
        Assert.Equal("jane3@example.com", result.Email);
        Assert.Equal("jane_3", result.Username);
    }

    [Fact]
    public async Task Replace_extracts_json_wrapped_in_prose_or_code_fences()
    {
        var replacer = ReplacerFor("Sure!\n```json\n{\"name\":\"Jane\",\"email\":\"j@example.com\",\"username\":\"j\"}\n```");

        PersonalData result = await replacer.Replace(new PersonalData(null, null, null), 1);

        Assert.Equal("Jane", result.Name);
        Assert.Equal("j1@example.com", result.Email);
    }

    [Fact]
    public async Task Requests_one_batch_and_serves_several_records_before_calling_again()
    {
        const string array =
            "[{\"name\":\"A A\",\"email\":\"a@example.com\",\"username\":\"a\"}," +
            "{\"name\":\"B B\",\"email\":\"b@example.com\",\"username\":\"b\"}," +
            "{\"name\":\"C C\",\"email\":\"c@example.com\",\"username\":\"c\"}]";
        var chat = ChatReturning(array);
        var replacer = new AiPersonalDataReplacer(chat,
            Options.Create(new AiReplacementOptions { BatchSize = 3 }), NullLogger<AiPersonalDataReplacer>.Instance);

        var emails = new HashSet<string>();
        for (var index = 0; index < 3; index++)
        {
            emails.Add((await replacer.Replace(new PersonalData(null, null, null), index)).Email!);
        }

        Assert.Equal(3, emails.Count);
        await chat.Received(1).GetChatResponseAsync(Arg.Any<Action<AIChatBuilder>>(),
            Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<CancellationToken>());

        // a fourth record empties the buffer and triggers a second batch call
        await replacer.Replace(new PersonalData(null, null, null), 3);
        await chat.Received(2).GetChatResponseAsync(Arg.Any<Action<AIChatBuilder>>(),
            Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replace_falls_back_to_a_templated_value_when_the_response_is_not_json()
    {
        var replacer = ReplacerFor("I'm sorry, I can't help with that.");

        PersonalData result = await replacer.Replace(new PersonalData(null, null, null), 5);

        Assert.Equal("user5", result.Name);
        Assert.Equal("user5@example.com", result.Email);
        Assert.Equal("user5", result.Username);
    }

    [Fact]
    public async Task Replace_falls_back_when_the_chat_service_throws()
    {
        var chat = Substitute.For<IAIChatService>();
        chat.GetChatResponseAsync(Arg.Any<Action<AIChatBuilder>>(), Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(new InvalidOperationException("boom")));

        PersonalData result = await CreateReplacer(chat).Replace(new PersonalData(null, null, null), 9);

        Assert.Equal("user9@example.com", result.Email);
    }

    private static AiPersonalDataReplacer ReplacerFor(string responseText) => CreateReplacer(ChatReturning(responseText));

    private static IAIChatService ChatReturning(string responseText)
    {
        var chat = Substitute.For<IAIChatService>();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText));
        chat.GetChatResponseAsync(Arg.Any<Action<AIChatBuilder>>(), Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(response));
        return chat;
    }

    private static AiPersonalDataReplacer CreateReplacer(IAIChatService chat)
        => new(chat, Options.Create(new AiReplacementOptions()), NullLogger<AiPersonalDataReplacer>.Instance);
}
