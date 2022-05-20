namespace Shiny.Net.Http;

public record QueryFilter(
    DirectionFilter Direction = DirectionFilter.Both,
    params string[] Ids
);
