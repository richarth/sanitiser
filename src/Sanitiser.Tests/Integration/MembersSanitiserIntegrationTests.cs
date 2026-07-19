using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Community.Sanitiser.Replacement;
using Umbraco.Community.Sanitiser.sanitisers;
using Umbraco.Community.Sanitiser.Tests.Support;
using Xunit;

namespace Umbraco.Community.Sanitiser.Tests.Integration;

public sealed class MembersSanitiserIntegrationTests
{
    [Fact]
    public async Task Anonymise_mode_replaces_every_pii_field_and_keeps_the_record()
    {
        IMember member = FakeMember(10, "Alice Real", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(member);

        await CreateSanitiser(memberService, SanitisationMode.Anonymise).Sanitise(Context());

        memberService.Received(1).Save(member);
        memberService.DidNotReceive().Delete(Arg.Any<IMember>());
        Assert.Equal("user0", member.Name);
        Assert.Equal("user0@example.com", member.Email);
        Assert.Equal("user0", member.Username);
    }

    [Fact]
    public async Task Delete_mode_scrubs_pii_before_deleting()
    {
        IMember member = FakeMember(10, "Alice Real", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(member);

        await CreateSanitiser(memberService, SanitisationMode.Delete).Sanitise(Context());

        memberService.Received(1).Delete(member);
        Assert.Equal("user0@example.com", member.Email);
        Assert.NotEqual("alice@real.com", member.Email);
    }

    [Fact]
    public async Task Skips_members_on_excluded_domains()
    {
        IMember excluded = FakeMember(11, "Bob", "bob@keep.com", "bob");
        IMember normal = FakeMember(12, "Alice", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(excluded, normal);

        await CreateSanitiser(memberService, SanitisationMode.Delete, domainsToExclude: "keep.com").Sanitise(Context());

        memberService.DidNotReceive().Delete(excluded);
        memberService.Received(1).Delete(normal);
        Assert.Equal("bob@keep.com", excluded.Email);
    }

    [Fact]
    public async Task Dry_run_makes_no_changes_but_reports_what_it_would_do()
    {
        IMember member = FakeMember(10, "Alice Real", "alice@real.com", "alice");
        IMemberService memberService = MemberServiceReturning(member);
        var logger = new ListLogger<MembersSanitiser>();

        await CreateSanitiser(memberService, SanitisationMode.Delete, logger: logger).Sanitise(Context(dryRun: true));

        memberService.DidNotReceive().Delete(Arg.Any<IMember>());
        memberService.DidNotReceive().Save(Arg.Any<IMember>());
        Assert.Equal("alice@real.com", member.Email);
        Assert.Contains(logger.Entries, e => e.Message.Contains("[DRY RUN]") && e.Message.Contains("10"));
    }

    [Fact]
    public async Task Anonymise_mode_clears_custom_member_properties_except_preserved()
    {
        IMember member = FakeMember(10, "Alice Real", "alice@real.com", "alice");
        IProperty address = FakeProperty("address");
        IProperty tier = FakeProperty("membershipTier");
        IProperty approved = FakeProperty("umbracoMemberApproved");
        var properties = new PropertyCollection(new[] { address, tier, approved });
        member.Properties.Returns(properties);
        IMemberService memberService = MemberServiceReturning(member);

        await CreateSanitiser(memberService, SanitisationMode.Anonymise, propertiesToPreserve: ["membershipTier"])
            .Sanitise(Context());

        member.Received().SetValue("address", null);
        member.DidNotReceive().SetValue("membershipTier", Arg.Any<object?>());
        // Built-in membership fields (umbracoMember* aliases) are account state, not personal data, so intact.
        member.DidNotReceive().SetValue("umbracoMemberApproved", Arg.Any<object?>());
    }

    private MembersSanitiser CreateSanitiser(IMemberService memberService, SanitisationMode mode, string domainsToExclude = "",
        ILogger<MembersSanitiser>? logger = null, string[]? propertiesToPreserve = null)
    {
        var options = Options.Create(new MembersSanitiserOptions
        {
            Enable = true,
            Mode = mode,
            DomainsToExclude = domainsToExclude,
            PropertiesToPreserve = propertiesToPreserve ?? []
        });
        var replacer = new TemplatePersonalDataReplacer(Options.Create(new TemplateReplacementOptions()));
        return new MembersSanitiser(options, replacer, memberService, logger ?? NullLogger<MembersSanitiser>.Instance);
    }

    private static SanitisationContext Context(bool dryRun = false) => new(dryRun, NullLogger.Instance);

    private static IMemberService MemberServiceReturning(params IMember[] members)
    {
        var service = Substitute.For<IMemberService>();
        long total;
        service.GetAll(0L, 0, out total)
            .ReturnsForAnyArgs(callInfo =>
            {
                callInfo[2] = (long)members.Length;
                return members;
            });
        return service;
    }

    private static IMember FakeMember(int id, string name, string email, string username)
    {
        var member = Substitute.For<IMember>();
        member.Id = id;
        member.Name = name;
        member.Email = email;
        member.Username = username;
        return member;
    }

    private static IProperty FakeProperty(string alias)
    {
        var property = Substitute.For<IProperty>();
        property.Alias.Returns(alias);
        return property;
    }
}
