using SQLite;
using System;


namespace Samples.SqliteGenerator
{
    public enum Gender
    {
        Male,
        Female
    }


    public class Customer
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Country { get; set; }
        public string ProvinceState { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
        //public string Gender { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
    }
}
