using System.Text.Json;

const string FilePath = "collection.json";
const string InsertSetsPath = "insert_sets.json";

var checklistDir = Environment.GetEnvironmentVariable("DUGOUT_CHECKLIST_DIR")
	?? Environment.GetEnvironmentVariable("TOPPS_CHECKLIST_DIR")
	?? "checklistInputs";
var checklistPdfPath = Environment.GetEnvironmentVariable("DUGOUT_CHECKLIST_PDF")
	?? Environment.GetEnvironmentVariable("TOPPS_CHECKLIST_PDF")
	?? Path.Combine(checklistDir, "2026_Topps_Series_1_Baseball_Checklist.pdf");
var oddsPdfPath = Environment.GetEnvironmentVariable("DUGOUT_ODDS_PDF")
	?? Environment.GetEnvironmentVariable("TOPPS_ODDS_PDF")
	?? Path.Combine(checklistDir, "2026_Topps_Baseball_Series_1_Odds.pdf");
var series2ChecklistPdfPath = Environment.GetEnvironmentVariable("DUGOUT_S2_CHECKLIST_PDF")
	?? Environment.GetEnvironmentVariable("TOPPS_S2_CHECKLIST_PDF")
	?? Path.Combine(checklistDir, "2026_Topps_Series_2_Baseball_Checklist_5-11.pdf");
var series2OddsPdfPath = Environment.GetEnvironmentVariable("DUGOUT_S2_ODDS_PDF")
	?? Environment.GetEnvironmentVariable("TOPPS_S2_ODDS_PDF")
	?? Path.Combine(checklistDir, "2026_Topps_Baseball_Series_2_Odds.pdf");
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

var cards = StorageService.LoadOrCreateCards(FilePath, jsonOptions, checklistPdfPath);
var insertSets = StorageService.LoadOrCreateInsertSets(InsertSetsPath, jsonOptions);

if (CommandHandler.NormalizeInsertSets(insertSets))
{
	StorageService.SaveInsertSets(InsertSetsPath, insertSets, jsonOptions);
}

var insertSetsChanged = false;
if (ChecklistPdfParser.HydrateInsertSetNumbersFromChecklist(insertSets, checklistPdfPath))
	insertSetsChanged = true;
if (ChecklistPdfParser.HydrateInsertSetNumbersFromChecklist(insertSets, series2ChecklistPdfPath))
	insertSetsChanged = true;
if (insertSetsChanged)
{
	StorageService.SaveInsertSets(InsertSetsPath, insertSets, jsonOptions);
}

StorageService.NormalizeCards(cards);
if (ChecklistPdfParser.HydratePlaceholderNamesFromChecklist(cards, checklistPdfPath))
{
	StorageService.SaveCards(FilePath, cards, jsonOptions);
}
if (ChecklistPdfParser.HydrateSeriesTwoCards(cards, series2ChecklistPdfPath))
{
	StorageService.SaveCards(FilePath, cards, jsonOptions);
}

var seenOddsKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var oddsEntries = OddsPdfParser.LoadOddsFromPdf(oddsPdfPath)
	.Concat(OddsPdfParser.LoadOddsFromPdf(series2OddsPdfPath))
	.Where(o => seenOddsKeys.Add($"{o.Name}|{o.OddsText}"))
	.OrderBy(o => o.OneIn)
	.ToList();

CliRouter.Run(cards, insertSets, oddsEntries, FilePath, InsertSetsPath, jsonOptions);
