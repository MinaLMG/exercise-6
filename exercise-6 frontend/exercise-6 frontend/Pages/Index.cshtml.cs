using exercise_4_frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace exercise_4_frontend.Pages
{
    public class IndexModel : PageModel
    {
        public HttpClient HttpClient = new();
        public List<Recipe> Recipes = new();
        public List<Category> CategoriesList = new();
        public Dictionary<Guid, string> categoriesNamesMap = new Dictionary<Guid, string>();
        [BindProperty(SupportsGet = true)]
        public string ReqResult { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Msg { get; set; }
        [BindProperty(SupportsGet = true)]
        public Guid ID { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Name { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Open { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Title { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Ingredients { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Instructions { get; set; }
        [BindProperty(SupportsGet = true)]
        public string Message { get; set; }
        [BindProperty(SupportsGet = true)]
        public Guid[] Categories { get; set; } = new Guid[0];
        private readonly ILogger<IndexModel> _logger;
        public IConfiguration Configuration;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        public async Task OnGet()
        {
            await ListItems();
        }

        public async Task ListItems()
        {
            var res = await HttpClient.GetAsync(Configuration["BaseUrl"] + "recipes");
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var inBetween = res.Content.ReadAsStringAsync().Result;
            List<Recipe> recipes = JsonSerializer.Deserialize<List<Recipe>>(inBetween, serializeOptions);
            this.Recipes = recipes;
            /* getting categories */
            res = await HttpClient.GetAsync(Configuration["BaseUrl"] + "categories");
            inBetween = res.Content.ReadAsStringAsync().Result;
            List<Category> categories = JsonSerializer.Deserialize<List<Category>>(inBetween, serializeOptions);
            this.CategoriesList = categories;
            /* storing some dictionaries*/
            //Dictionary<string, Guid> categoriesMap = new Dictionary<string, Guid>();
            for (int i = 0; i < CategoriesList.Count; i++)
            {
                //categoriesMap[categories[i].Name] = categories[i].ID;
                this.categoriesNamesMap[CategoriesList[i].ID] = CategoriesList[i].Name;
            }
        }
        public async Task<IActionResult> OnPostCreateRecipe()
        {
            Recipe toAdd = new Recipe("", new(), new(), new());

            toAdd.Title = Title.Trim();
            foreach (Guid category in Categories)
            {
                toAdd.Categories.Add(category);
            }

            toAdd.Instructions = new();
            // using the method
            String[] strlist = Instructions.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            foreach (String s in strlist)
            {
                if (s.Trim() != "")
                {
                    toAdd.Instructions.Add(s.Trim());
                }
            }
            strlist = Ingredients.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            foreach (String s in strlist)
            {
                if (s.Trim() != "")
                    toAdd.Ingredients.Add(s.Trim());
            }


            var temp = JsonSerializer.Serialize(toAdd);
            var res = await HttpClient.PostAsync(Configuration["BaseUrl"] + "recipes", new StringContent(temp, Encoding.UTF8, "application/json"));
            if ((int)res.StatusCode == 200)
                return Redirect("/index?ReqResult=success&Msg=your recipe has been added successfully");
            else
                return RedirectToPage("/index", new { ReqResult = "failure", Msg = "something went wrong with your request .. check your data and try again", open = "add", title = Title, instructions = Instructions, ingredients = Ingredients, categories = Categories });
        }

        public async Task<IActionResult> OnPostUpdateRecipe()
        {
            Recipe toEdit = new Recipe("", new(), new(), new());
            toEdit.ID = ID;
            toEdit.Title = Title.Trim();
            foreach (Guid category in Categories)
            {
                toEdit.Categories.Add(category);
            }

            toEdit.Instructions = new();
            // using the method
            Instructions = Instructions.Trim();
            String[] strlist = Instructions.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            foreach (String s in strlist)
            {
                if (s.Trim() != "")
                    toEdit.Instructions.Add(s.Trim());
            }
            Ingredients = Ingredients.Trim();
            strlist = Ingredients.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            foreach (String s in strlist)
            {
                if (s.Trim() != "")
                    toEdit.Ingredients.Add(s.Trim());
            }


            var temp = JsonSerializer.Serialize(toEdit);
            var res = await HttpClient.PutAsync(Configuration["BaseUrl"] + "recipes/" + ID, new StringContent(temp, Encoding.UTF8, "application/json"));
            if ((int)res.StatusCode == 200)
                return Redirect("/index?ReqResult=success&Msg=the recipe has been updated successfully");
            else
                return RedirectToPage("/index", new { ReqResult = "failure", Msg = "something went wrong with your request .. review your data and try again", open = "edit", title = Title, id = ID, instructions = Instructions, ingredients = Ingredients, categories = Categories }); /*+ "&instructions=" + Instructions + "&ingredients=" + Ingredients);*/
        }

        public async Task<IActionResult> OnPostDeleteRecipe()
        {
            var res = await HttpClient.DeleteAsync(Configuration["BaseUrl"] + "recipes/" + ID);
            if ((int)res.StatusCode == 200)
                return Redirect("/index?ReqResult=success&Msg=the recipe has been deleted successfully");
            else
                return RedirectToPage("/index",
                    new
                    {
                        ReqResult = "failure",
                        Msg = "something went wrong with your request .. you can retry after some seconds",
                        open = "delete",
                        id = ID,
                    });
        }

    }
}