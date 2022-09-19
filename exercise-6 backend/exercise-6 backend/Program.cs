using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins("https://localhost:7113/",
                               "https://localhost:5113/",
                               "https://minalmg-2.azurewebsites.net").AllowAnyHeader()
                                                .AllowAnyMethod()
                                                .AllowAnyOrigin(); ;
        });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = String.Empty;
});
app.UseCors();

Data data = new Data(app);
Pages pages = new Pages(data);
pages.CategoryPages(app);
pages.RecipePages(app);
app.Run();

public class Data
{
    public List<Category> Categories { get; set; } = new();
    public List<Recipe> Recipes { get; set; } = new();
    public Dictionary<string, Guid> CategoriesMap { get; set; }
    public Dictionary<Guid, string> CategoriesNamesMap { get; set; }
    public string RecipesLoc { get; set; }
    public string CategoriesLoc { get; set; }
    public JsonSerializerOptions Options { get; set; }

    public void WriteInFolder(string text, string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine(text);
        }
    }

    public Data(WebApplication app)
    {
        this.Options = new JsonSerializerOptions { WriteIndented = true };
        string mainPath = Environment.CurrentDirectory;

        if (app.Environment.IsDevelopment())
        {
            this.CategoriesLoc = $@"{mainPath}\..\categories.json";
        }
        else
        {
            this.CategoriesLoc = $@"{mainPath}\categories.json";
        }
        string categoriesString = File.ReadAllText(this.CategoriesLoc);
        this.Categories = JsonSerializer.Deserialize<List<Category>>(categoriesString);
        /****/
        this.CategoriesMap = new Dictionary<string, Guid>();
        this.CategoriesNamesMap = new Dictionary<Guid, string>();
        for (int i = 0; i < this.Categories.Count; i++)
        {
            this.CategoriesMap[this.Categories[i].Name] = this.Categories[i].ID;
            this.CategoriesNamesMap[this.Categories[i].ID] = this.Categories[i].Name;
        }
        if (app.Environment.IsDevelopment())
        {
            this.RecipesLoc = $@"{mainPath}\..\recipes.json";
        }
        else
        {
            this.RecipesLoc = $@"{mainPath}\recipes.json";
        }
        string recipesString = File.ReadAllText(this.RecipesLoc);
        this.Recipes = JsonSerializer.Deserialize<List<Recipe>>(recipesString);
    }
    public Category AddCategory(Category to_add)
    {
        to_add.ID = Guid.NewGuid();
        this.Categories.Add(to_add);
        this.CategoriesMap[to_add.Name] = to_add.ID;
        this.CategoriesNamesMap[to_add.ID] = to_add.Name;
        this.WriteInFolder(JsonSerializer.Serialize(this.Categories, this.Options), this.CategoriesLoc);
        return to_add;
    }
    public Category EditCategory(Guid id, Category newCategory)
    {
        Category toEdit = this.Categories.Single(x => x.ID == id);
        toEdit.Name = newCategory.Name;
        this.WriteInFolder(JsonSerializer.Serialize(this.Categories, this.Options), this.CategoriesLoc);
        return toEdit;
    }
    public Category DeleteCategory(Guid id)
    {
        Category toDelete = this.Categories.Single(x => x.ID == id);
        this.Categories.Remove(toDelete);
        foreach (Recipe recipe in this.Recipes)
        {
            try
            {
                Guid toDelete2 = recipe.Categories.Single(x => x == id);
                recipe.Categories.Remove(toDelete2);
            }
            catch (Exception e)
            {

            }
        }
        this.WriteInFolder(JsonSerializer.Serialize(this.Categories, this.Options), this.CategoriesLoc);
        this.WriteInFolder(JsonSerializer.Serialize(this.Recipes, this.Options), this.RecipesLoc);
        return toDelete;
    }
    public Recipe EditRecipe(Guid id, Recipe newRecipe)
    {
        Recipe toEdit = this.Recipes.Single(x => x.ID == id);
        toEdit.Title = newRecipe.Title;
        toEdit.Instructions = newRecipe.Instructions;
        toEdit.Ingredients = newRecipe.Ingredients;
        toEdit.Categories = newRecipe.Categories;
        this.WriteInFolder(JsonSerializer.Serialize(this.Recipes, this.Options), this.RecipesLoc);
        return toEdit;
    }
    public Recipe DeleteRecipe(Guid id)
    {
        Recipe toDelete = this.Recipes.Single(x => x.ID == id);
        this.Recipes.Remove(toDelete);
        this.WriteInFolder(JsonSerializer.Serialize(this.Recipes, this.Options), this.RecipesLoc);
        return toDelete;
    }
    public void AddRecipe(Recipe to_add)
    {
        this.Recipes.Add(to_add);
        this.WriteInFolder(JsonSerializer.Serialize(this.Recipes, this.Options), this.RecipesLoc);
    }

}
public class Pages
{
    public Data Data { get; set; }
    public IResult CheckCategory(Category c, string action, Guid id = new Guid())
    {
        if (c.Name == null || c.Name.Trim() == "")
        {
            return Results.BadRequest("name must be a non-empty field");
            //at client: responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        }
        c.Name = c.Name.Trim();
        switch (action)
        {
            case "add":
                Category added = this.Data.AddCategory(c);
                return Results.Json(new { c.Name, added.ID });

            case "edit":
                Category toEdit = Data.EditCategory(id, c);
                return Results.Json(toEdit);
        }
        // should not be called 
        return Results.Ok();
    }

    public IResult CheckRecipe(Recipe r, string action, Guid id = new Guid())
    {
        if (r.Title == null || r.Title.Trim() == "")
        {
            return Results.BadRequest("title must be a non-empty field");
        }
        r.Title = r.Title.Trim();

        List<string> instructions = new();
        foreach (var instruction in r.Instructions)
        {
            if (instruction.Trim() != "")
            {
                instructions.Add(instruction);
            }
        }
        if (instructions.Count == 0)
        {
            return Results.BadRequest("the recipe must have a non-zero number of instructions");
        }
        r.Instructions = instructions;

        List<string> ingredients = new();
        foreach (var ingredient in r.Ingredients)
        {
            if (ingredient.Trim() != "")
            {
                ingredients.Add(ingredient);
            }
        }
        if (ingredients.Count == 0)
        {
            return Results.BadRequest("the recipe must have a non-zero number of instructions");
        }
        r.Ingredients = ingredients;

        List<Guid> categories = new();
        foreach (var category in r.Categories)
        {
            try
            {
                string temp = Data.CategoriesNamesMap[category];
            }
            catch (Exception e)
            {
                return Results.BadRequest("the recipe must have a real and existing categories");
            }
        }
        switch (action)
        {
            case "add":
                r.ID = Guid.NewGuid();
                Data.AddRecipe(r);
                return Results.Json(new { r.Title, r.Ingredients, r.Instructions, r.Categories, r.ID });

            case "edit":
                Recipe toEdit = Data.EditRecipe(id, r);
                return Results.Json(toEdit);
        }
        // should not be called 
        return Results.Ok();

    }
    public IResult CreateCategory([FromBody] Category c)
    {
        return CheckCategory(c, "add");
    }

    public IResult EditCategory(Guid id, [FromBody] Category c)
    {
        return CheckCategory(c, "edit", id);
    }
    public IResult DeleteCategory(Guid id)
    {
        Category toDelete = Data.DeleteCategory(id);

        return Results.Json(toDelete);
    }

    public IResult CreateRecipe([FromBody] Recipe r)
    {
        return CheckRecipe(r, "add");
    }

    public IResult EditRecipe(Guid id, [FromBody] Recipe r)
    {
        return CheckRecipe(r, "edit", id);

    }
    public IResult DeleteRecipe(Guid id)
    {
        Recipe toDelete = Data.DeleteRecipe(id);
        return Results.Json(toDelete);
    }
    public void CategoryPages(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/categories", () => Results.Json(Data.Categories));
        endpoints.MapPost("/categories", CreateCategory);
        endpoints.MapPut("/categories/{id}", EditCategory);
        endpoints.MapDelete("/categories/{id}", DeleteCategory);
    }

    public void RecipePages(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/recipes", () => Results.Json(Data.Recipes));
        endpoints.MapPost("/recipes", CreateRecipe);
        endpoints.MapPut("/recipes/{id}", EditRecipe);
        endpoints.MapDelete("/recipes/{id}", DeleteRecipe);

    }

    public Pages(Data data) { this.Data = data; }
}
public class Category
{
    public string Name { get; set; }
    public Guid ID { get; set; }
}
public class Recipe
{
    public string Title { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public List<string> Instructions { get; set; } = new();
    public List<Guid> Categories { get; set; } = new();
    public Guid ID { get; set; }

}