using System.Threading.Tasks;
using CRM.Sheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace CRM.Web.Pages.Sheets;

[Authorize]
public class SettingsModel : AbpPageModel
{
    private readonly SheetConnectionService _conns;
    public SettingsModel(SheetConnectionService conns) => _conns = conns;

    public string ScriptText { get; set; } = "";

    public Task OnGetAsync() => Load(false);
    public async Task<IActionResult> OnPostRegenerateAsync() { await Load(true); return Page(); }

    private async Task Load(bool regenerate)
    {
        var (key, secret) = await _conns.GetOrCreateAsync(regenerate);
        ScriptText = Template.Replace("__URL__", $"{Request.Scheme}://{Request.Host}/api/sheets/webhook")
                             .Replace("__KEY__", key).Replace("__SECRET__", secret);
    }

    private const string Template = """
const WEBHOOK_URL = '__URL__', KEY = '__KEY__', SECRET = '__SECRET__', MAX_ROWS = 200;
// Columns: A ID | B Name | C Company | D Email | E Phone | F Budget | G Remarks
//          H CustomerType | I LeadSource | J LeadStage | K Service | L AssignedUser (username)

function setup() {  // run ONCE and authorize
  ScriptApp.getProjectTriggers().forEach(t => ScriptApp.deleteTrigger(t));
  ScriptApp.newTrigger('onSheetEdit').forSpreadsheet(SpreadsheetApp.getActive()).onEdit().create();
}

function onSheetEdit(e) {
  const sh = e.range.getSheet();
  if (sh.getIndex() !== 1) return;                       // first tab only
  const first = Math.max(e.range.getRow(), 2);           // skip header
  const last = Math.min(e.range.getLastRow(), first + MAX_ROWS - 1);
  if (last < first) return;

  const vals = sh.getRange(first, 1, last - first + 1, 12).getValues();
  const ids = [], rows = [];
  let newId = false;
  vals.forEach(v => {
    if (v[1] && !v[0]) { v[0] = Utilities.getUuid(); newId = true; }
    ids.push([v[0]]);
    if (!v[1]) return;
    rows.push({ externalId: String(v[0]), name: String(v[1]), companyName: String(v[2]), email: String(v[3]),
      phone: String(v[4]), estimatedBudget: Number(v[5]) || 0, remarks: String(v[6]),
      customerType: String(v[7]), leadSource: String(v[8]), leadStage: String(v[9]),
      service: String(v[10]), assignedUser: String(v[11]) });
  });
  if (newId) sh.getRange(first, 1, ids.length, 1).setValues(ids);   // one write
  if (!rows.length) return;

  const body = JSON.stringify({ ts: Date.now(), rows: rows });
  const sig = Utilities.computeHmacSha256Signature(body, SECRET)
    .map(b => ((b < 0 ? b + 256 : b)).toString(16).padStart(2, '0')).join('');
  UrlFetchApp.fetch(WEBHOOK_URL, { method: 'post', contentType: 'application/json', payload: body,
    headers: { 'X-Sheet-Key': KEY, 'X-Sheet-Signature': sig }, muteHttpExceptions: true });
}
""";
}