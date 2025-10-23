using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UI.Agents;
using UI.Models;
using System.Linq;

namespace UI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IAgentOrchestrator _orchestrator;

        public IndexModel(IAgentOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        [BindProperty]
        public UserInput Input { get; set; } = new();

        // Raw comma-separated preferences from the form
        [BindProperty]
        public string PreferencesRaw { get; set; } = string.Empty;

        public OrchestrationResult? Result { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // parse comma separated preferences into the list
            Input.DietaryPreferences = PreferencesRaw?.Split(',')
                .Select(s => s?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList() ?? new();

            Result = await _orchestrator.RunAsync(Input);
            return Page();
        }
    }
}
