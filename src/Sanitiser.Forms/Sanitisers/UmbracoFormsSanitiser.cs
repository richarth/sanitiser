using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Community.Sanitiser.Forms.Configuration;
using Umbraco.Community.Sanitiser.Forms.Models;
using Umbraco.Community.Sanitiser.sanitisers;

namespace Umbraco.Community.Sanitiser.Forms.Sanitisers;

public class UmbracoFormsSanitiser : ISanitiser
{
    private readonly IScopeProvider _scopeProvider;
    private readonly SanitiserFormsOptions _options;

    public UmbracoFormsSanitiser(IOptions<SanitiserFormsOptions> options, IScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
        _options = options.Value;
    }

    public async Task Sanitise()
    {
        await DeleteFormEntries();
    }

    public bool IsEnabled()
    {
        return _options.UmbracoFormsSanitiser?.Enable ?? false;
    }

    private async Task DeleteFormEntries()
    {
        using IScope scope = _scopeProvider.CreateScope();

        await scope.Database.DeleteManyAsync<UmbracoFormsRecordsAudit>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecordDataBit>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecordDataDateTime>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecordDataInteger>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecordDataLongString>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecordDataString>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecordFields>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecordWorkflowAudit>().Execute();
        await scope.Database.DeleteManyAsync<UmbracoFormsRecords>().Execute();

        scope.Complete();
    }
}
