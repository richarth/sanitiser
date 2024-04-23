using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.Sanitiser.Configuration;
using Umbraco.Forms.Core.Models;
using Umbraco.Forms.Core.Persistence.Dtos;
using Umbraco.Forms.Core.Services;

namespace Umbraco.Community.Sanitiser.sanitisers;

public class UmbracoFormsSanitiser(
    IOptions<SanitiserOptions> options,
    IFormService formService,
    IRecordService recordService,
    IUmbracoContextFactory umbracoContextFactory)
{
    private readonly SanitiserOptions _options = options.Value;

    public Task Sanitise()
    {
        umbracoContextFactory.EnsureUmbracoContext();

        IEnumerable<Form> forms = GetFormsOnSite();

        foreach (Form form in forms)
        {
            IEnumerable<Record> formRecords = GetRecordsForForm(form);

            foreach (Record record in formRecords)
            {
                recordService.Delete(record, form);
            }
        }

        return Task.CompletedTask;
    }

    public bool IsEnabled()
    {
        return _options.UmbracoFormsSanitiser?.Enable ?? false;
    }

    private IEnumerable<Form> GetFormsOnSite()
    {
        return formService.Get();
    }

    private IEnumerable<Record> GetRecordsForForm(Form form)
    {
        return recordService.GetAllRecords(form, false);
    }
}
