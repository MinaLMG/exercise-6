using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using exercise_6_backend.Models;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Filters;
using exercise_6_backend.Services.UserServices;

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
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Standard Authorization header",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});
IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("Token.json")
            .AddEnvironmentVariables()
            .Build();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
            config.GetRequiredSection("Token").Get<string>())
            ),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();
//builder.Services.AddScoped<IUserInterface, UserService>();
//builder.Services.AddHttpContextAccessor();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = String.Empty;
});
app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true) // allow any origin
    .AllowCredentials()); // allow credentials
app.UseAuthentication();
app.UseAuthorization();
Data data = new Data(app);
Pages pages = new Pages(data);
pages.CategoryPages(app);
pages.RecipePages(app);
pages.RegisterPages(app);
app.Run();

public class Data
{
    private IConfiguration configuration;

    public List<Category> Categories { get; set; } = new();
    public List<Recipe> Recipes { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<UserDTO> UserDTOs { get; set; } = new();
    public Dictionary<string, Guid> CategoriesMap { get; set; }
    public Dictionary<Guid, string> CategoriesNamesMap { get; set; }
    public string RecipesLoc { get; set; }
    public string CategoriesLoc { get; set; }
    public string UsersLoc { get; set; }
    public JsonSerializerOptions Options { get; set; }

    public void WriteInFolder(string text, string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine(text);
        }
    }
    public void CreatePasswordHash(string password, out byte[] PasswordSalt, out byte[] PasswordHash)
    {
        using (var hmac = new HMACSHA512())
        {
            PasswordSalt = hmac.Key;
            PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

        }
    }
    public bool VerifyPasswordHash(string password, byte[] PasswordSalt, byte[] PasswordHash)
    {
        using (var hmac = new HMACSHA512(PasswordSalt))
        {
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return hash.SequenceEqual(PasswordHash);
        }
    }

    public string CreateToken(User user)
    {
        List<Claim> claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name,user.UserName)
        };
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
            configuration.GetRequiredSection("Token").Get<string>())
            );
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: cred);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return jwt;
    }
    public RefreshToken GenerateRefreshToken()
    {
        var refershToken = new RefreshToken()
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.Now.AddDays(7),
            CreatedAt = DateTime.Now
        };
        return refershToken;
    }
    public User RefreshToken(string refreshToken)
    {
        try
        {
            User user = Users.Single(u => u.RefreshToken == refreshToken);
            return user;
        }
        catch
        {
            return null;
        }

    }
    public CookieOptions SetRefreshToken(RefreshToken refreshToken)
    {
        var cookie = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.Now.AddDays(7),
        };
        return cookie;
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
        /******/

        if (app.Environment.IsDevelopment())
        {
            this.UsersLoc = $@"{mainPath}\..\users.json";
        }
        else
        {
            this.UsersLoc = $@"{mainPath}\users.json";
        }
        string usersString = File.ReadAllText(this.UsersLoc);
        this.Users = JsonSerializer.Deserialize<List<User>>(usersString);
        // Build a config object, using env vars and JSON providers.
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("Token.json")
            .AddEnvironmentVariables()
            .Build();
        this.configuration = config;
    }
    public IResult AddCategory(Category to_add)
    {
        to_add.ID = Guid.NewGuid();
        this.Categories.Add(to_add);
        this.CategoriesMap[to_add.Name] = to_add.ID;
        this.CategoriesNamesMap[to_add.ID] = to_add.Name;
        this.WriteInFolder(JsonSerializer.Serialize(this.Categories, this.Options), this.CategoriesLoc);
        return Results.Json(to_add);
    }
    public IResult EditCategory(Guid id, Category newCategory)
    {
        Category toEdit;
        try
        {
            toEdit = this.Categories.Single(x => x.ID == id);
        }
        catch
        {
            return Results.NotFound("No category exists with that id");
        }
        toEdit.Name = newCategory.Name;
        this.WriteInFolder(JsonSerializer.Serialize(this.Categories, this.Options), this.CategoriesLoc);
        return Results.Json(toEdit);

    }
    public IResult DeleteCategory(Guid id)
    {
        Category toDelete;
        try
        {
            toDelete = this.Categories.Single(x => x.ID == id);
        }
        catch
        {
            return Results.NotFound("No category exists with that id");
        }
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
        return Results.Json(toDelete);
    }
    public IResult EditRecipe(Guid id, Recipe newRecipe)
    {
        Recipe toEdit;
        try
        {
            toEdit = this.Recipes.Single(x => x.ID == id);
        }
        catch
        {
            return Results.NotFound("No Recipe exists with that id");

        }
        toEdit.Title = newRecipe.Title;
        toEdit.Instructions = newRecipe.Instructions;
        toEdit.Ingredients = newRecipe.Ingredients;
        toEdit.Categories = newRecipe.Categories;
        this.WriteInFolder(JsonSerializer.Serialize(this.Recipes, this.Options), this.RecipesLoc);
        return Results.Json(toEdit);
    }
    public IResult DeleteRecipe(Guid id)
    {

        Recipe toDelete;
        try
        {
            toDelete = this.Recipes.Single(x => x.ID == id);
        }
        catch
        {
            return Results.NotFound("No Recipe exists with that id");
        }
        this.Recipes.Remove(toDelete);
        this.WriteInFolder(JsonSerializer.Serialize(this.Recipes, this.Options), this.RecipesLoc);
        return Results.Json(toDelete);
    }
    public void AddRecipe(Recipe to_add)
    {
        this.Recipes.Add(to_add);
        this.WriteInFolder(JsonSerializer.Serialize(this.Recipes, this.Options), this.RecipesLoc);
    }
    public User RegisterUser(UserDTO user)
    {
        CreatePasswordHash(user.Password, out byte[] passwordSalt, out byte[] passwordHash);
        User response = new();
        response.UserName = user.UserName;
        response.PasswordHash = passwordHash;
        response.PasswordSalt = passwordSalt;
        this.Users.Add(response);
        this.WriteInFolder(JsonSerializer.Serialize(this.Users, this.Options), this.UsersLoc);
        return response;
    }
    public User LoginUser(UserDTO user, RefreshToken refreshToken, out bool r)
    {
        try
        {
            User u = Users.Single(u => u.UserName == user.UserName);
            r = VerifyPasswordHash(user.Password, u.PasswordSalt, u.PasswordHash);
            u.TokenCreatedAt = refreshToken.CreatedAt;
            u.TokenExpiresAt = refreshToken.ExpiresAt;
            u.RefreshToken = refreshToken.Token;
            this.WriteInFolder(JsonSerializer.Serialize(this.Users, this.Options), this.UsersLoc);
            return u;
        }
        catch
        {
            r = false;
            return null;
        }
    }
    public User RefreshUserToken(string userName, RefreshToken refreshToken, out bool r)
    {
        try
        {
            User u = Users.Single(u => u.UserName == userName);
            u.TokenCreatedAt = refreshToken.CreatedAt;
            u.TokenExpiresAt = refreshToken.ExpiresAt;
            u.RefreshToken = refreshToken.Token;
            this.WriteInFolder(JsonSerializer.Serialize(this.Users, this.Options), this.UsersLoc);
            r = true;
            return u;
        }
        catch
        {
            r = false;
            return null;
        }
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
                return Data.AddCategory(c);


            case "edit":
                return Data.EditCategory(id, c);
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
                return Data.EditRecipe(id, r);
        }
        // should not be called 
        return Results.Ok();

    }
    [HttpPost, Authorize]
    public IResult CreateCategory([FromBody] Category c)
    {
        return CheckCategory(c, "add");
    }
    [Authorize]
    public IResult EditCategory(Guid id, [FromBody] Category c)
    {
        return CheckCategory(c, "edit", id);
    }
    [Authorize]
    public IResult DeleteCategory(Guid id)
    {
        return Data.DeleteCategory(id);
    }

    //[HttpPost,Authorize] 
    [Authorize]
    public IResult CreateRecipe([FromBody] Recipe r)
    {
        return CheckRecipe(r, "add");
    }
    [Authorize]
    public IResult EditRecipe(Guid id, [FromBody] Recipe r)
    {
        return CheckRecipe(r, "edit", id);
    }
    [HttpDelete, Authorize]
    public IResult DeleteRecipe(Guid id)
    {
        return Data.DeleteRecipe(id);
    }
    public IResult RegisterUser([FromBody] UserDTO u, HttpContext httpContext)
    {
        User user = Data.RegisterUser(u);
        return Results.Ok("");
    }
    public IResult LoginUser([FromBody] UserDTO u, HttpResponse response)
    {
        RefreshToken refreshToken = Data.GenerateRefreshToken();
        CookieOptions cookie = Data.SetRefreshToken(refreshToken);
        User user = Data.LoginUser(u, refreshToken, out bool r);
        if (user == null)
            return Results.NotFound("no user with this username exists");
        if (!r)
        {
            return Results.BadRequest("username and password don't match ");
        }
        string token = Data.CreateToken(user);
        response.Cookies.Append("refreshToken", refreshToken.Token, cookie);
        return Results.Json(token);
    }
    public async Task<IResult> RefreshToken(HttpRequest httpRequest, HttpResponse httpResponse)
    {
        var refreshToken = httpRequest.Cookies["refreshToken"];
        if (refreshToken == null)
            return Results.BadRequest("No refresh token is sent!");
        User user = Data.RefreshToken(refreshToken);
        if (user == null)
            return Results.BadRequest("refresh token doesn't exist!");
        if (user.TokenExpiresAt < DateTime.Now)
            return Results.BadRequest("this refresh token has expired !");

        RefreshToken newRefreshToken = Data.GenerateRefreshToken();
        CookieOptions cookie = Data.SetRefreshToken(newRefreshToken);
        User u = Data.RefreshUserToken(user.UserName, newRefreshToken, out bool r);
        string token = Data.CreateToken(u);
        httpResponse.Cookies.Append("refreshToken", newRefreshToken.Token, cookie);
        return Results.Json(token);
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
    public void RegisterPages(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/register", RegisterUser);
        endpoints.MapPost("/login", LoginUser);
        endpoints.MapPost("/refresh-token", RefreshToken);
    }

    public Pages(Data data) { this.Data = data; }
}