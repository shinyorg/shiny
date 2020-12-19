using System;
using System.Threading.Tasks;
using Bogus;
using SQLite;


namespace Samples.SqliteGenerator
{
    public static class Generator
    {
        public static async Task CreateSqlite(string path, int customerCount)
        {
            var conn = new SQLiteAsyncConnection(path);
            await conn.CreateTableAsync<Customer>().ConfigureAwait(false);

            //https://github.com/bchavez/Bogus
            var faker = new Faker<Customer>()
                //.RuleFor(u => u.Gender, f => f.PickRandom<Gender>())
                .RuleFor(x => x.FirstName, (f, x) => f.Name.FirstName())
                .RuleFor(x => x.LastName, (f, x) => f.Name.LastName())
                .RuleFor(x => x.Email, (f, x) => f.Internet.Email(x.FirstName, x.LastName))
                .RuleFor(x => x.Phone, (f, x) => f.Phone.PhoneNumber())
                .RuleFor(x => x.Country, (f, x) => f.Address.Country())
                .RuleFor(x => x.ProvinceState, (f, x) => f.Address.State())
                .RuleFor(x => x.City, (f, x) => f.Address.City())
                .RuleFor(x => x.Address, (f, x) => f.Address.StreetAddress())
                .RuleFor(x => x.PostalCode, (f, x) => f.Address.ZipCode());

            var data = faker.Generate(customerCount);
            await conn.InsertAllAsync(data, true);
            await conn.CloseAsync();
        }
    }
}
