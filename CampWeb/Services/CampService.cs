using CampWeb.Models;

namespace CampWeb.Services;

public interface ICampService
{
    Task<List<Camp>> GetAllCampsAsync();
    Task<List<Camp>> GetPopularCampsAsync();
    Task<Camp?> GetCampByIdAsync(int id);
    Task<List<Camp>> FilterCampsAsync(string? type = null, string? ageGroup = null, int? maxPrice = null);
}

public class CampService : ICampService
{
    private readonly List<Camp> _camps;

    public CampService()
    {
        _camps = InitializeCamps();
    }

    public Task<List<Camp>> GetAllCampsAsync()
    {
        return Task.FromResult(_camps);
    }

    public Task<List<Camp>> GetPopularCampsAsync()
    {
        return Task.FromResult(_camps.Take(3).ToList());
    }

    public Task<Camp?> GetCampByIdAsync(int id)
    {
        var camp = _camps.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(camp);
    }

    public Task<List<Camp>> FilterCampsAsync(string? type = null, string? ageGroup = null, int? maxPrice = null)
    {
        var filtered = _camps.AsQueryable();

        if (!string.IsNullOrEmpty(type))
            filtered = filtered.Where(c => c.Type == type);

        if (!string.IsNullOrEmpty(ageGroup))
            filtered = filtered.Where(c => MatchesAgeGroup(c.AgeGroup, ageGroup));

        if (maxPrice.HasValue)
            filtered = filtered.Where(c => c.Price <= maxPrice.Value);

        return Task.FromResult(filtered.ToList());
    }

    private static bool MatchesAgeGroup(string campAge, string filterAge)
    {
        return filterAge switch
        {
            "6-9" => campAge.Contains("6") || campAge.Contains("7") || campAge.Contains("8") || campAge.Contains("9"),
            "10-13" => campAge.Contains("10") || campAge.Contains("11") || campAge.Contains("12") || campAge.Contains("13"),
            "14-17" => campAge.Contains("14") || campAge.Contains("15") || campAge.Contains("16") || campAge.Contains("17"),
            _ => true
        };
    }

    private static List<Camp> InitializeCamps()
    {
        return new List<Camp>
        {
            new()
            {
                Id = 1,
                Name = "Dobrodružný tábor Šumava",
                Location = "Šumava, 35 km od Plzně",
                Type = "adventure",
                Price = 5900,
                AvailableSpots = 8,
                AgeGroup = "8-15",
                ShortDescription = "Turistika v krásné přírodě Šumavy s nocováním pod hvězdami.",
                Description = "Prožijte nezapomenutelné dobrodružství v srdci Šumavy! Náš tábor kombinuje aktivní turistiku s poznáváním přírody. Děti se naučí orientaci v terénu, založení táboráku a základní outdoorové dovednosti.",
                Latitude = 49.2847,
                Longitude = 13.5441,
                Images = new List<string>
                {
                    "https://images.unsplash.com/photo-1504851149312-7a075b496cc7?w=800",
                    "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800"
                },
                Activities = new List<string> { "Turistika", "Táboráky", "Orientace v terénu", "Příroda", "Stanování" }
            },
            new()
            {
                Id = 2,
                Name = "Sportovní tábor Plzeň",
                Location = "Plzeň - Doubravka",
                Type = "sport",
                Price = 4500,
                AvailableSpots = 2,
                AgeGroup = "10-16",
                ShortDescription = "Fotbal, volejbal, atletika a další sporty.",
                Description = "Sportovní tábor pro mladé nadšence sportu. Program zahrnuje fotbal, volejbal, atletiku, plavání a další sporty. Kvalifikovaní trenéři a moderní sportovní zázemí.",
                Latitude = 49.7473,
                Longitude = 13.3776,
                Images = new List<string>
                {
                    "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800",
                    "https://images.unsplash.com/photo-1596464716127-f2a82984de30?w=800"
                },
                Activities = new List<string> { "Fotbal", "Volejbal", "Atletika", "Plavání", "Basketbal" }
            },
            new()
            {
                Id = 3,
                Name = "Kreativní tábor",
                Location = "Plzeň centrum",
                Type = "creative",
                Price = 5200,
                AvailableSpots = 12,
                AgeGroup = "6-14",
                ShortDescription = "Výtvarné dílo, hudba, divadlo a rukodělné aktivity.",
                Description = "Tábor pro malé umělce! Kreativní dílny, výtvarné techniky, hudební nástroje, divadelní představení a mnoho dalšího. Rozvíjíme fantazii a tvořivost dětí.",
                Latitude = 49.7384,
                Longitude = 13.3736,
                Images = new List<string>
                {
                    "https://images.unsplash.com/photo-1513475382585-d06e58bcb0e0?w=800",
                    "https://images.unsplash.com/photo-1503095396549-807759245b35?w=800"
                },
                Activities = new List<string> { "Malování", "Keramika", "Hudba", "Divadlo", "Rukodělné práce" }
            },
            new()
            {
                Id = 4,
                Name = "Vodácký tábor Berounka",
                Location = "Berounka, 15 km",
                Type = "water",
                Price = 5800,
                AvailableSpots = 5,
                AgeGroup = "12-17",
                ShortDescription = "Splutí Berounky s výukou vodáctví a bezpečnosti na vodě.",
                Description = "Vodácký tábor na řece Berounce. Děti se naučí základy kanoistiky, bezpečnost na vodě a prožijí dobrodružství na vodě. Kvalifikovaní instruktoři a kompletní vybavení.",
                Latitude = 49.8347,
                Longitude = 13.4712,
                Images = new List<string>
                {
                    "https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=800",
                    "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800"
                },
                Activities = new List<string> { "Kanoistika", "Raftování", "Plavání", "Rybaření", "Táboráky u vody" }
            },
            new()
            {
                Id = 5,
                Name = "Horský tábor Brdy",
                Location = "Brdy, 25 km od Plzně",
                Type = "adventure",
                Price = 6200,
                AvailableSpots = 0,
                AgeGroup = "10-16",
                ShortDescription = "Turistika v krásné přírodě Brd s nocováním pod hvězdami.",
                Description = "Horský tábor v Brdech s turistikou, horolezectvím a poznáváním přírody. Noční pochody, pozorování hvězd a survival dovednosti.",
                Latitude = 49.6847,
                Longitude = 13.7441,
                Images = new List<string>
                {
                    "https://images.unsplash.com/photo-1486022119627-aca3ac10c9d2?w=800"
                },
                Activities = new List<string> { "Horolezectví", "Turistika", "Survival", "Pozorování hvězd" }
            },
            new()
            {
                Id = 6,
                Name = "Vědecký tábor",
                Location = "Plzeň - Technické muzeum",
                Type = "science",
                Price = 4900,
                AvailableSpots = 15,
                AgeGroup = "8-15",
                ShortDescription = "Experimenty, robotika a objevování světa vědy.",
                Description = "Vědecký tábor pro mladé badatele. Experimenty, robotika, programování, astronomie a mnoho dalších vědeckých aktivit v moderních laboratořích.",
                Latitude = 49.7384,
                Longitude = 13.3736,
                Images = new List<string>
                {
                    "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800",
                    "https://images.unsplash.com/photo-1518709268805-4e9042af2176?w=800"
                },
                Activities = new List<string> { "Experimenty", "Robotika", "Programování", "Astronomie", "Chemie" }
            }
        };
    }
}