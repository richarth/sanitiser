using Bogus;
using Umbraco.Community.Sanitiser.Replacement;

namespace Umbraco.Community.Sanitiser;

public class FakerPersonalDataReplacer : IPersonalDataReplacer
{
    // Registered as a singleton and only ever called sequentially during startup sanitisation,
    // so a single (non-thread-safe) Faker instance is fine.
    private readonly Faker _faker = new();

    public Task<PersonalData> Replace(PersonalData original, int index)
    {
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();

        // Pin the domain to the RFC 2606 reserved example.com so generated addresses can never
        // reach a real inbox, and use the index as a unique suffix so replacements never collide
        // (users/members have unique email/username constraints). The underscore separator keeps
        // the index an unambiguous token so two records can never merge into the same username.
        var email = _faker.Internet.Email(firstName, lastName, "example.com", index.ToString());
        var username = $"{_faker.Internet.UserName(firstName, lastName)}_{index}";

        return Task.FromResult(new PersonalData($"{firstName} {lastName}", email, username));
    }
}
