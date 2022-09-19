namespace exercise_4_frontend.Models
{
    public class Category
    {
        public string Name { get; set; }
        public Guid ID { get; set; }
        public Category(string x)
        {
            this.Name = x;
            this.ID = Guid.NewGuid() ;
        }
        public Category() { }

    }
}
