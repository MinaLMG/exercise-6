namespace exercise_6_backend.Models
{

    public class Recipe
    {
        public string Title { get; set; }
        public List<string> Ingredients { get; set; } = new();
        public List<string> Instructions { get; set; } = new();
        public List<Guid> Categories { get; set; } = new();
        public Guid ID { get; set; }
    }
}
