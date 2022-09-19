namespace exercise_4_frontend.Models
{
    public class Recipe
    {
        public string Title { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public List<string> Instructions { get; set; } = new();
        public List<Guid> Categories { get; set; } = new();
        public Guid ID { get; set; }
        public Recipe(string title, List<string> ingredients, List<string> instructions, List<Guid> categories)
        {
            this.Title = title;
            this.Ingredients = ingredients;
            this.Instructions = instructions;
            this.Categories = categories;
            this.ID = Guid.NewGuid();
        }
        public Recipe() { }
    }
}
